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
        public string ProductName { get; set; }

        [Column("product_description")]
        public string? ProductDescription { get; set; } // optional description

        [Column("image_path")]
        public string? ImagePath { get; set; } // optional image path

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Column("date_updated")]
        public DateTime? DateUpdated { get; set; } // optional update date

    }
}

           