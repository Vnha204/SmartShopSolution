using System.ComponentModel.DataAnnotations;

namespace SmartShop.Models
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        [Required, MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        public DateTime OtpExpiry { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
    }
}
