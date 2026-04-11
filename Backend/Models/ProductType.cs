using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("product_types")]
    public class ProductType
    {
        [Key]
        [Column("product_type_id")]
        public int ProductTypeId { get; set; }

        [Required]
        [Column("product_type_name")]
        [StringLength(255)]
        public string ProductTypeName { get; set; } = string.Empty;

        [Column("product_type_description", TypeName = "text")]
        public string? ProductTypeDescription { get; set; }

        [Column("is_active", TypeName = "tinyint(1)")]
        public bool IsActive { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; }

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }
    }
}