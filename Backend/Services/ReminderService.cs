using System.Linq;
using Backend.Models;
using FluentFTP;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend.Services
{
    // An email attachment already resolved to bytes in memory, ready to send.
    public class ReminderAttachmentData
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
    }

    // Shared FTP (attachment storage) + SMTP (sending) logic used by both the
    // one-off "Send Email Reminder" endpoint and the recurring reminder
    // background scheduler, so the two paths can't drift apart.
    public class ReminderService
    {
        private readonly FtpSettings _ftpSettings;
        private readonly EmailSettings _emailSettings;

        public ReminderService(IOptions<FtpSettings> ftpSettings, IOptions<EmailSettings> emailSettings)
        {
            _ftpSettings = ftpSettings.Value;
            _emailSettings = emailSettings.Value;
        }

        // Uploads a file to the FTP "reminder attachments" folder and returns the
        // unique name it was stored under.
        public async Task<string> UploadAttachmentAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            var storedFileName = $"{Guid.NewGuid():N}{ext}";
            var remotePath = _ftpSettings.ReminderAttachmentsPath.TrimEnd('/') + "/" + storedFileName;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var ftp = CreateClient();
            await ftp.Connect();
            var status = await ftp.UploadStream(ms, remotePath, FtpRemoteExists.Overwrite, createRemoteDir: true);
            await ftp.Disconnect();

            if (status == FtpStatus.Failed)
                throw new Exception($"FTP upload failed for '{file.FileName}'.");

            return storedFileName;
        }

        public async Task<byte[]> DownloadAttachmentAsync(string storedFileName)
        {
            var remotePath = _ftpSettings.ReminderAttachmentsPath.TrimEnd('/') + "/" + storedFileName;

            using var ftp = CreateClient();
            await ftp.Connect();
            using var ms = new MemoryStream();
            var ok = await ftp.DownloadStream(ms, remotePath);
            await ftp.Disconnect();

            if (!ok)
                throw new Exception($"FTP download failed for '{storedFileName}'.");

            return ms.ToArray();
        }

        public async Task DeleteAttachmentAsync(string storedFileName)
        {
            var remotePath = _ftpSettings.ReminderAttachmentsPath.TrimEnd('/') + "/" + storedFileName;
            try
            {
                using var ftp = CreateClient();
                await ftp.Connect();
                await ftp.DeleteFile(remotePath);
                await ftp.Disconnect();
            }
            catch
            {
                // Best-effort cleanup — a stray file left on the FTP server is harmless.
            }
        }

        public async Task SendAsync(List<string> to, List<string>? cc, string subject, string body, List<ReminderAttachmentData>? attachments = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));

            foreach (var addr in to)
                message.To.Add(MailboxAddress.Parse(addr));

            foreach (var addr in cc ?? new List<string>())
                message.Cc.Add(MailboxAddress.Parse(addr));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = BuildHtmlBody(body), TextBody = body ?? string.Empty };
            foreach (var att in attachments ?? new List<ReminderAttachmentData>())
            {
                ContentType contentType;
                try { contentType = ContentType.Parse(att.ContentType ?? "application/octet-stream"); }
                catch { contentType = ContentType.Parse("application/octet-stream"); }

                bodyBuilder.Attachments.Add(att.FileName, att.Bytes, contentType);
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }

        // The body is composer-typed plain text (with real line breaks), not HTML.
        // Split it into paragraphs on blank lines and wrap each in a <p>, turning any
        // remaining single line breaks into <br> — relying on CSS (e.g. white-space:
        // pre-wrap) isn't safe here, since Outlook's Word-based renderer and other
        // clients ignore it and can end up collapsing or hiding the whole body.
        private static string BuildHtmlBody(string? body)
        {
            var text = (body ?? string.Empty).Replace("\r\n", "\n");
            var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.None)
                .Select(p => System.Net.WebUtility.HtmlEncode(p).Replace("\n", "<br>"))
                .Select(p => $"<p style=\"margin:0 0 14px 0;\">{p}</p>");

            return $"<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:1.6;\">{string.Join(string.Empty, paragraphs)}</div>";
        }

        private AsyncFtpClient CreateClient()
        {
            var ftp = new AsyncFtpClient(_ftpSettings.Host, _ftpSettings.User, _ftpSettings.Password, _ftpSettings.Port);
            // Accept HostGator's shared-hosting SSL certificate, same as product image uploads.
            ftp.Config.ValidateAnyCertificate = true;
            ftp.Config.EncryptionMode = FtpEncryptionMode.Auto;
            return ftp;
        }
    }
}
