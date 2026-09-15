using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("company_phones")]
    public class CompanyPhone
    {
        [Key]
        [Column("phone_id")]
        public int PhoneId { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("phone_number")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [JsonIgnore]
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }
    }
}
