using Backend.Data;
using Backend.Models;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailSettings _emailSettings;

        public ContactController(AppDbContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
        }

        // POST: api/contact
        [HttpPost]
        public async Task<IActionResult> SubmitContact([FromBody] ContactSubmission submission)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Save to database
            submission.SubmittedAt = DateTime.UtcNow;
            _context.ContactSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // Send email notification
            try
            {
                await SendEmailNotification(submission);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }

            return Ok(new { message = "Thank you for your enquiry! We will get back to you shortly." });
        }

            private async Task SendEmailNotification(ContactSubmission submission)
        {
            // --- Notification to Arrow Instruments ---
            var notification = new MimeMessage();
            notification.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            notification.To.Add(MailboxAddress.Parse(_emailSettings.ToEmail));
            notification.Subject = $"New Enquiry from {submission.Name} — Arrow Instruments";
            notification.Body = new TextPart("html")
            {
                Text = $@"
            <html><body style='font-family:Arial,sans-serif;color:#333'>
            <h2 style='color:#770202'>New Enquiry — Arrow Instruments</h2>
            <table style='border-collapse:collapse;width:100%;max-width:600px'>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold;width:30%'>Name</td><td style='padding:8px;border:1px solid #ddd'>{submission.Name}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold'>Company</td><td style='padding:8px;border:1px solid #ddd'>{submission.CompanyName ?? "—"}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold'>Email</td><td style='padding:8px;border:1px solid #ddd'>{submission.Email}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold'>Phone</td><td style='padding:8px;border:1px solid #ddd'>{submission.Phone ?? "—"}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold'>Message</td><td style='padding:8px;border:1px solid #ddd'>{submission.Message ?? "—"}</td></tr>
                <tr><td style='padding:8px;border:1px solid #ddd;background:#f9f9f9;font-weight:bold'>Submitted At</td><td style='padding:8px;border:1px solid #ddd'>{submission.SubmittedAt:dd MMM yyyy, hh:mm tt} UTC</td></tr>
            </table>
            </body></html>"
            };

            // --- Auto-reply to customer ---
            var autoReply = new MimeMessage();
            autoReply.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            autoReply.To.Add(MailboxAddress.Parse(submission.Email));
            autoReply.Subject = "Thank you for contacting Arrow Instruments";
            autoReply.Body = new TextPart("html")
            {
                Text = $@"
            <html><body style='font-family:Arial,sans-serif;color:#333'>
            <h2 style='color:#770202'>Thank you for your enquiry!</h2>
            <p>Dear {submission.Name},</p>
            <p>We have received your enquiry and will get back to you within <strong>24 hours</strong>.</p>
            <br/>
            <p>Best regards,<br/>
            <strong>Arrow Instruments Team</strong><br/>
            📞 +91-8008030606<br/>
            ✉️ enquiries@arrowinstruments.in<br/>
            🌐 www.arrowinstruments.in</p>
            <p style='color:#888;font-size:12px;font-style:italic'>Accuracy. Precision. Service. Durability.</p>
            </body></html>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _emailSettings.SmtpHost,
                _emailSettings.SmtpPort,
                SecureSocketOptions.SslOnConnect
            );
            await smtp.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            await smtp.SendAsync(notification);
            await smtp.SendAsync(autoReply);
            await smtp.DisconnectAsync(true);
        }
    }
}