using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CCAPI.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(256)")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        // Связи (может быть NULL)
        public int? ClientID { get; set; }
        public int? DriverID { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Навигационные свойства (для EF Core)
        public Client? Client { get; set; }
        public Driver? Driver { get; set; }

        // ← Добавляем это!
        public Role? Role { get; set; } // Навигационное свойство к Role
    }
}