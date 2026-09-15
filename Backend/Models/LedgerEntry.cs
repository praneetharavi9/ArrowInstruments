using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("ledger_entries")]
    public class LedgerEntry
    {
        [Key]
        [Column("entry_id")]
        public int EntryId { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Column("entry_date")]
        public DateTime EntryDate { get; set; }

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("type")]
        [MaxLength(50)]
        public string? Type { get; set; }

        [Column("trans_no")]
        [MaxLength(50)]
        public string? TransNo { get; set; }

        [Column("debit", TypeName = "decimal(12,2)")]
        public decimal Debit { get; set; }

        [Column("credit", TypeName = "decimal(12,2)")]
        public decimal Credit { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
    }
}
