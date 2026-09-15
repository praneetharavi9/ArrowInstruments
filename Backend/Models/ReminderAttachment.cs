using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("reminder_attachments")]
    public class ReminderAttachment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("reminder_schedule_id")]
        public int ReminderScheduleId { get; set; }

        // Original file name, shown to the recipient.
        [Required]
        [Column("file_name")]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        // Unique name the file is stored under on the FTP server.
        [Required]
        [Column("stored_file_name")]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Column("content_type")]
        [MaxLength(100)]
        public string? ContentType { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ForeignKey("ReminderScheduleId")]
        public ReminderSchedule? ReminderSchedule { get; set; }
    }
}
