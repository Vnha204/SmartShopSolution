using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; } = null!;

        [Required, MaxLength(50)]
        public string OrderCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "cod";

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "unpaid";

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        public DateTime UpdatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        [MaxLength(100)]
        public string? PayOSPaymentLinkId { get; set; }

        public long? PayOSOrderCode { get; set; }

        [MaxLength(500)]
        public string? PayOSCheckoutUrl { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
    public static class OrderStatus
    {
        public const string Pending = "pending";
        public const string Confirmed = "confirmed";
        public const string Processing = "processing";
        public const string Shipping = "shipping";
        public const string Delivered = "delivered";
        public const string Cancelled = "cancelled";

        public static Dictionary<string, string> DisplayNames => new()
        {
            [Pending] = "Chờ xác nhận",
            [Confirmed] = "Đã xác nhận",
            [Processing] = "Đang xử lý",
            [Shipping] = "Đang giao",
            [Delivered] = "Đã giao",
            [Cancelled] = "Đã huỷ",
        };
    }
}
