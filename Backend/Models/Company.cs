using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("companies")]
    public class Company
    {
        [Key]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("company_name")]
        [MaxLength(255)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [Column("gst_number")]
        [MaxLength(50)]
        public string GstNumber { get; set; } = string.Empty;

        [Column("address1")]
        [MaxLength(255)]
        public string? Address1 { get; set; }

        [Column("address2")]
        [MaxLength(255)]
        public string? Address2 { get; set; }

        [Column("city")]
        [MaxLength(100)]
        public string? City { get; set; }

        [Column("state")]
        [MaxLength(100)]
        public string? State { get; set; }

        [Column("zipcode")]
        [MaxLength(20)]
        public string? Zipcode { get; set; }

        [Column("opening_balance", TypeName = "decimal(12,2)")]
        public decimal OpeningBalance { get; set; } = 0;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }

        // Navigation properties
        public List<CompanyPhone> Phones { get; set; } = new List<CompanyPhone>();
        public List<CompanyEmail> Emails { get; set; } = new List<CompanyEmail>();
        public List<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
        public List<ReminderSchedule> ReminderSchedules { get; set; } = new List<ReminderSchedule>();
    }
}
