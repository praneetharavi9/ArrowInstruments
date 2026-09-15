using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("company_emails")]
    public class CompanyEmail
    {
        [Key]
        [Column("email_id")]
        public int EmailId { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("email_address")]
        [MaxLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [JsonIgnore]
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
    }
}
