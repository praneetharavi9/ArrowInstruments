using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("reminder_schedules")]
    public class ReminderSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        // "daily" | "weekly" | "monthly" | "yearly"
        [Required]
        [Column("frequency")]
        [MaxLength(20)]
        public string Frequency { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        // Time of day (wall-clock, as entered by the admin) the reminder should go out at.
        [Column("send_time")]
        public TimeSpan SendTime { get; set; } = new TimeSpan(9, 0, 0);

        // When true, a fresh ledger statement PDF (opening balance through the
        // send date) is generated and attached each time this schedule fires.
        [Column("attach_ledger_statement")]
        public bool AttachLedgerStatement { get; set; }

        // JSON array of recipient email strings, e.g. ["a@x.com","b@y.com"]
        [Required]
        [Column("to_emails")]
        public string ToEmails { get; set; } = "[]";

        [Column("cc_emails")]
        public string? CcEmails { get; set; }

        [Required]
        [Column("subject")]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column("body")]
        public string Body { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_sent_at")]
        public DateTime? LastSentAt { get; set; }

        [Column("next_run_at")]
        public DateTime NextRunAt { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }

        [JsonIgnore]
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        public List<ReminderAttachment> Attachments { get; set; } = new List<ReminderAttachment>();
    }
}
