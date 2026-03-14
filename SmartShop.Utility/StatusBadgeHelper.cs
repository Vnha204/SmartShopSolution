using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Utility
{
    public static class StatusBadgeHelper
    {
        public static string GetOrderStatusBadge(string status) => status switch
        {
            "pending" => "bg-warning text-dark",
            "confirmed" => "bg-info text-white",
            "processing" => "bg-primary",
            "shipping" => "bg-info",
            "delivered" => "bg-success",
            "cancelled" => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetPaymentStatusBadge(string status) => status switch
        {
            "paid" => "bg-success",
            "unpaid" => "bg-warning text-dark",
            "refunded" => "bg-secondary",
            _ => "bg-secondary"
        };

        public static string GetStockBadge(int stock) => stock switch
        {
            0 => "bg-danger",
            < 10 => "bg-warning text-dark",
            _ => "bg-success"
        };
    }
}
