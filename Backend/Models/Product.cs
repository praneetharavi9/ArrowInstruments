using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("product_type_id")]
        public int ProductTypeId { get; set; }

        [Required]
        [Column("product_name")]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Column("product_description")]
        public string? ProductDescription { get; set; }

        [Column("image_path")]
        public string? ImagePath { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; }

        // Navigation property - specs for this product
        public List<ProductSpec> Specs { get; set; } = new List<ProductSpec>();
    }
}