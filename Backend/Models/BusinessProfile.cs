using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    // ArrowInstruments' own letterhead + bank details (as opposed to Company,
    // which holds customers). Single-row table — always id 1.
    [Table("business_profile")]
    public class BusinessProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("company_name")]
        [MaxLength(255)]
        public string CompanyName { get; set; } = string.Empty;

        [Column("certification")]
        [MaxLength(255)]
        public string? Certification { get; set; }

        [Column("business_line")]
        [MaxLength(255)]
        public string? BusinessLine { get; set; }

        [Column("address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Column("mobile_numbers")]
        [MaxLength(100)]
        public string? MobileNumbers { get; set; }

        [Column("email")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Column("bank_name")]
        [MaxLength(100)]
        public string? BankName { get; set; }

        [Column("bank_branch")]
        [MaxLength(100)]
        public string? BankBranch { get; set; }

        [Column("bank_account_no")]
        [MaxLength(50)]
        public string? BankAccountNo { get; set; }

        [Column("bank_ifsc_code")]
        [MaxLength(20)]
        public string? BankIfscCode { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }
    }
}
