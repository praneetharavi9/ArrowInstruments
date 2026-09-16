using System.Text.Json;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    // Polls for reminder schedules that are due and sends them. Runs for the
    // lifetime of the app, so it only actually fires on a host that stays up
    // (fine on Railway; a serverless/sleeping host would need a real cron trigger).
    public class ReminderSchedulerService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

        // Lower bound used when auto-attaching a ledger statement to a recurring
        // reminder — a full running statement to date, not just a recent window.
        private static readonly DateTime EarliestStatementDate = new DateTime(2000, 1, 1);

        private readonly IServiceProvider _services;
        private readonly ILogger<ReminderSchedulerService> _logger;

        public ReminderSchedulerService(IServiceProvider services, ILogger<ReminderSchedulerService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(CheckInterval);
            do
            {
                try
                {
                    await ProcessDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder scheduler run failed.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task ProcessDueRemindersAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reminderService = scope.ServiceProvider.GetRequiredService<ReminderService>();
            var documentService = scope.ServiceProvider.GetRequiredService<LedgerDocumentService>();

            // NextRunAt/SendTime are naive IST wall-clock values (see IndiaTime),
            // so "now" must be expressed the same way, not as true UTC — otherwise
            // a schedule can look overdue (or not-yet-due) by up to 5.5 hours.
            var now = IndiaTime.NowAsIst;
            var due = await context.ReminderSchedules
                .Include(r => r.Attachments)
                .Where(r => r.IsActive && r.NextRunAt <= now)
                .ToListAsync(ct);

            if (due.Count == 0) return;

            foreach (var schedule in due)
            {
                if (schedule.EndDate != null && schedule.EndDate.Value.Date < now.Date)
                {
                    schedule.IsActive = false;
                    continue;
                }

                try
                {
                    var to = ParseJsonList(schedule.ToEmails);
                    var cc = ParseJsonList(schedule.CcEmails);

                    var attachments = new List<ReminderAttachmentData>();
                    foreach (var att in schedule.Attachments)
                    {
                        var bytes = await reminderService.DownloadAttachmentAsync(att.StoredFileName);
                        attachments.Add(new ReminderAttachmentData
                        {
                            FileName = att.FileName,
                            ContentType = att.ContentType,
                            Bytes = bytes
                        });
                    }

                    if (schedule.AttachLedgerStatement)
                    {
                        var statement = await documentService.BuildStatementDataAsync(schedule.CompanyId, EarliestStatementDate, now.Date);
                        attachments.Add(new ReminderAttachmentData
                        {
                            FileName = $"{statement.CustomerName} Ledger Statement.pdf",
                            ContentType = "application/pdf",
                            Bytes = documentService.BuildPdf(statement)
                        });
                    }

                    await reminderService.SendAsync(to, cc, schedule.Subject, schedule.Body, attachments);

                    schedule.LastSentAt = now;

                    // Advance past "now" (rather than by exactly one period) so a
                    // schedule that was due for a while doesn't stay stuck as due.
                    do
                    {
                        schedule.NextRunAt = ComputeNextRun(schedule.NextRunAt, schedule.Frequency);
                    } while (schedule.NextRunAt <= now);

                    if (schedule.EndDate != null && schedule.NextRunAt.Date > schedule.EndDate.Value.Date)
                        schedule.IsActive = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder schedule {Id}", schedule.Id);
                    // Back off a day so a persistent failure doesn't retry every 30
                    // minutes — but keep the schedule's configured send time (not
                    // "now"), otherwise a failure at, say, 1:22 AM during a poll
                    // tick permanently knocks the schedule off its intended
                    // 9:00 PM slot instead of just delaying it by a day.
                    schedule.NextRunAt = now.Date.AddDays(1).Add(schedule.SendTime);

                    if (schedule.EndDate != null && schedule.NextRunAt.Date > schedule.EndDate.Value.Date)
                        schedule.IsActive = false;
                }
            }

            await context.SaveChangesAsync(ct);
        }

        // Internal (not private) so RemindersController can reuse it to roll a
        // brand-new schedule's first NextRunAt forward if it's already due the
        // moment it's created (e.g. the admin picked a time already in the past).
        internal static DateTime ComputeNextRun(DateTime from, string frequency)
        {
            return frequency switch
            {
                "daily" => from.AddDays(1),
                "weekly" => from.AddDays(7),
                "monthly" => from.AddMonths(1),
                "yearly" => from.AddYears(1),
                _ => from.AddDays(7)
            };
        }

        private static List<string> ParseJsonList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
            catch { return new List<string>(); }
        }
    }
}
