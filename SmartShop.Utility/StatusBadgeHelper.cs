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
    }
}
