using System.ComponentModel.DataAnnotations;

namespace SmartShop.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(300)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        public bool IsRead { get; set; } = false;
    }
}
