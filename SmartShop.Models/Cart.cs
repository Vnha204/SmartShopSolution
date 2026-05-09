using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class Cart
    {
        public int CartId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? AppUser { get; set; }

        public DateTime CreatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        public DateTime UpdatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
