using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/reminders")]
    [Authorize(Roles = "admin")]
    public class RemindersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ReminderService _reminderService;
        private readonly LedgerDocumentService _documentService;

        public RemindersController(AppDbContext context, ReminderService reminderService, LedgerDocumentService documentService)
        {
            _context = context;
            _reminderService = reminderService;
            _documentService = documentService;
        }

        // POST: api/reminders/customers/5/send  (multipart/form-data — one-off email)
        [HttpPost("customers/{companyId}/send")]
        public async Task<IActionResult> SendReminder(int companyId, [FromForm] SendReminderForm form)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            var to = ParseEmailList(form.To);
            var cc = ParseEmailList(form.Cc);

            var error = ValidateRecipients(to, cc);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            if (string.IsNullOrWhiteSpace(form.Subject))
                return BadRequest(new { success = false, message = "Subject is required." });

            var attachments = new List<ReminderAttachmentData>();
            if (form.Files != null)
            {
                foreach (var file in form.Files)
                {
                    if (file.Length == 0) continue;
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    attachments.Add(new ReminderAttachmentData
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Bytes = ms.ToArray()
                    });
                }
            }

            if (form.AttachLedgerStatement)
            {
                var ledgerStart = form.LedgerStartDate?.Date ?? new DateTime(company.DateCreated.Year, 1, 1);
                var ledgerEnd = form.LedgerEndDate?.Date ?? DateTime.UtcNow.Date;
                var statement = await _documentService.BuildStatementDataAsync(companyId, ledgerStart, ledgerEnd);
                attachments.Add(new ReminderAttachmentData
                {
                    FileName = $"{company.CompanyName} Ledger Statement.pdf",
                    ContentType = "application/pdf",
                    Bytes = _documentService.BuildPdf(statement)
                });
            }

            try
            {
                await _reminderService.SendAsync(to, cc, form.Subject.Trim(), form.Body ?? string.Empty, attachments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Failed to send email: {ex.Message}" });
            }

            await SaveNewCompanyEmailsAsync(companyId, to.Concat(cc));

            return Ok(new { success = true, message = "Reminder email sent." });
        }

        // POST: api/reminders/customers/5/schedules  (multipart/form-data — recurring reminder)
        [HttpPost("customers/{companyId}/schedules")]
        public async Task<IActionResult> CreateSchedule(int companyId, [FromForm] ScheduleReminderForm form)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            var to = ParseEmailList(form.To);
            var cc = ParseEmailList(form.Cc);

            var recipientError = ValidateRecipients(to, cc);
            if (recipientError != null)
                return BadRequest(new { success = false, message = recipientError });

            var frequency = form.Frequency?.Trim().ToLowerInvariant();
            if (frequency != "daily" && frequency != "weekly" && frequency != "monthly" && frequency != "yearly")
                return BadRequest(new { success = false, message = "Frequency must be daily, weekly, monthly, or yearly." });

            if (form.StartDate == default)
                return BadRequest(new { success = false, message = "Start date is required." });

            if (form.EndDate != null && form.EndDate.Value.Date < form.StartDate.Date)
                return BadRequest(new { success = false, message = "End date cannot be before the start date." });

            var sendTimeRaw = form.SendTime?.Trim();
            TimeSpan sendTime;
            if (string.IsNullOrWhiteSpace(sendTimeRaw))
            {
                sendTime = new TimeSpan(9, 0, 0);
            }
            else if (!TimeSpan.TryParseExact(sendTimeRaw, new[] { "hh\\:mm", "h\\:mm" }, System.Globalization.CultureInfo.InvariantCulture, out sendTime))
            {
                return BadRequest(new { success = false, message = "Send time must be in HH:mm format." });
            }

            if (string.IsNullOrWhiteSpace(form.Subject))
                return BadRequest(new { success = false, message = "Subject is required." });

            if (string.IsNullOrWhiteSpace(form.Body))
                return BadRequest(new { success = false, message = "Body is required." });

            // NextRunAt/SendTime are naive IST wall-clock values (see IndiaTime),
            // so if the admin's picked date+time has already passed by the time
            // this request lands (e.g. it was 7:50 PM they meant for later today,
            // but it's already past that), roll forward to the next real
            // occurrence instead of firing on the very next scheduler poll.
            var nowIst = IndiaTime.NowAsIst;
            var nextRunAt = form.StartDate.Date.Add(sendTime);
            while (nextRunAt <= nowIst)
            {
                nextRunAt = ReminderSchedulerService.ComputeNextRun(nextRunAt, frequency);
            }

            var schedule = new ReminderSchedule
            {
                CompanyId = companyId,
                Frequency = frequency,
                StartDate = form.StartDate.Date,
                EndDate = form.EndDate?.Date,
                SendTime = sendTime,
                AttachLedgerStatement = form.AttachLedgerStatement,
                ToEmails = JsonSerializer.Serialize(to),
                CcEmails = cc.Count > 0 ? JsonSerializer.Serialize(cc) : null,
                Subject = form.Subject.Trim(),
                Body = form.Body,
                IsActive = true,
                NextRunAt = nextRunAt,
                DateCreated = DateTime.UtcNow
            };

            _context.ReminderSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            if (form.Files != null)
            {
                foreach (var file in form.Files)
                {
                    if (file.Length == 0) continue;

                    string storedName;
                    try
                    {
                        storedName = await _reminderService.UploadAttachmentAsync(file);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { success = false, message = $"Attachment upload failed: {ex.Message}" });
                    }

                    _context.ReminderAttachments.Add(new ReminderAttachment
                    {
                        ReminderScheduleId = schedule.Id,
                        FileName = file.FileName,
                        StoredFileName = storedName,
                        ContentType = file.ContentType,
                        DateCreated = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            await SaveNewCompanyEmailsAsync(companyId, to.Concat(cc));

            return Ok(new { success = true, message = "Reminder schedule created.", scheduleId = schedule.Id });
        }

        // GET: api/reminders/customers/5/schedules
        [HttpGet("customers/{companyId}/schedules")]
        public async Task<IActionResult> GetSchedules(int companyId)
        {
            var schedules = await _context.ReminderSchedules
                .Include(r => r.Attachments)
                .Where(r => r.CompanyId == companyId)
                .OrderByDescending(r => r.DateCreated)
                .ToListAsync();

            var result = schedules.Select(r => new
            {
                r.Id,
                r.CompanyId,
                r.Frequency,
                startDate = r.StartDate,
                endDate = r.EndDate,
                sendTime = r.SendTime.ToString(@"hh\:mm"),
                r.AttachLedgerStatement,
                to = ParseJsonList(r.ToEmails),
                cc = ParseJsonList(r.CcEmails),
                r.Subject,
                r.Body,
                r.IsActive,
                r.LastSentAt,
                r.NextRunAt,
                r.DateCreated,
                attachments = r.Attachments.Select(a => new { a.Id, a.FileName }).ToList()
            });

            return Ok(result);
        }

        // PUT: api/reminders/schedules/12/toggle  (pause/resume)
        [HttpPut("schedules/{id}/toggle")]
        public async Task<IActionResult> ToggleSchedule(int id)
        {
            var schedule = await _context.ReminderSchedules.FindAsync(id);
            if (schedule == null)
                return NotFound(new { success = false, message = "Schedule not found." });

            schedule.IsActive = !schedule.IsActive;
            schedule.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, isActive = schedule.IsActive });
        }

        // DELETE: api/reminders/schedules/12
        [HttpDelete("schedules/{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.ReminderSchedules
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (schedule == null)
                return NotFound(new { success = false, message = "Schedule not found." });

            foreach (var att in schedule.Attachments)
                await _reminderService.DeleteAttachmentAsync(att.StoredFileName);

            _context.ReminderSchedules.Remove(schedule); // cascades to reminder_attachments rows
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Schedule deleted." });
        }

        // Persists any To/Cc address that isn't already saved on the company yet.
        private async Task SaveNewCompanyEmailsAsync(int companyId, IEnumerable<string> addresses)
        {
            var existing = await _context.CompanyEmails
                .Where(e => e.CompanyId == companyId)
                .Select(e => e.EmailAddress)
                .ToListAsync();

            var existingSet = existing.Select(e => e.ToLowerInvariant()).ToHashSet();
            var nextOrder = existing.Count;

            var toAdd = addresses
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(a => !existingSet.Contains(a.ToLowerInvariant()))
                .ToList();

            if (toAdd.Count == 0) return;

            foreach (var addr in toAdd)
            {
                _context.CompanyEmails.Add(new CompanyEmail
                {
                    CompanyId = companyId,
                    EmailAddress = addr,
                    IsPrimary = false,
                    DisplayOrder = nextOrder++
                });
            }

            await _context.SaveChangesAsync();
        }

        private static List<string> ParseEmailList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> ParseJsonList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
            catch { return new List<string>(); }
        }

        private static string? ValidateRecipients(List<string> to, List<string> cc)
        {
            if (to.Count == 0)
                return "At least one recipient (To) is required.";

            foreach (var addr in to.Concat(cc))
            {
                if (!IsValidEmail(addr))
                    return $"'{addr}' is not a valid email address.";
            }

            return null;
        }

        private static bool IsValidEmail(string addr)
        {
            try { _ = new System.Net.Mail.MailAddress(addr); return true; }
            catch { return false; }
        }
    }

    public class SendReminderForm
    {
        public string To { get; set; } = string.Empty;   // comma-separated
        public string? Cc { get; set; }                   // comma-separated
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public List<IFormFile>? Files { get; set; }
        public bool AttachLedgerStatement { get; set; }
        public DateTime? LedgerStartDate { get; set; }
        public DateTime? LedgerEndDate { get; set; }
    }

    public class ScheduleReminderForm
    {
        public string Frequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SendTime { get; set; }              // "HH:mm", defaults to 09:00
        public bool AttachLedgerStatement { get; set; }     // attach a fresh ledger PDF each send
        public string To { get; set; } = string.Empty;    // comma-separated
        public string? Cc { get; set; }                    // comma-separated
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<IFormFile>? Files { get; set; }
    }
}
