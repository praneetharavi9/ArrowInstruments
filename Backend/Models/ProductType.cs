using System;
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

        [Column("product_type_name")]
        [Required]
        [StringLength(255)] // You can adjust max length if known
        public string ProductTypeName { get; set; }

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
