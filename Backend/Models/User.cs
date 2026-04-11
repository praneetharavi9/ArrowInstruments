using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("first_name")]
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [Column("last_name")]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Column("role")]
        public string Role { get; set; } = "customer";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("profile_image_url")]
        [MaxLength(500)]
        public string? ProfileImageUrl { get; set; }

        [Column("phone")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("password_reset_token")]
        [MaxLength(255)]
        public string? PasswordResetToken { get; set; }

        [Column("password_reset_expires")]
        public DateTime? PasswordResetExpires { get; set; }
    }
}
