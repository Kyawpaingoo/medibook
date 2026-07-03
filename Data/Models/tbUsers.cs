using Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models
{
    [Table("tbUsers")]
    public class tbUsers
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }
        [Required]
        public required UserRole Role { get; set; }
        public Guid? Doctor_Id { get; set; }
        public string? Refresh_Token { get; set; }
        public DateTime? Refresh_Token_Expiry { get; set; }
        [Required]
        public required DateTimeOffset Created_At { get; set; }

        public tbDoctors? Doctor { get; set; }
    }
}
