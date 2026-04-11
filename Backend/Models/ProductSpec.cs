using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Table("product_specs")]
    public class ProductSpec
    {
        [Key]
        [Column("spec_id")]
        public int SpecId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("spec_name")]
        [MaxLength(100)]
        public string SpecName { get; set; } = string.Empty;

        [Required]
        [Column("spec_value", TypeName = "text")]
        public string SpecValue { get; set; } = string.Empty;

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [JsonIgnore]  
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}