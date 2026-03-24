using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartShop.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPriceSnapshot { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
