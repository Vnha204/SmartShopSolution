namespace SmartShop.Services
{
    public class PayOSPaymentResult
    {
        public bool    Success        { get; set; }
        public string? CheckoutUrl    { get; set; }
        public string? PaymentLinkId  { get; set; }
        public long?   OrderCode      { get; set; }
        public string? ErrorMessage   { get; set; }
    }

    public interface IPayOSService
    {
        Task<PayOSPaymentResult> CreatePaymentLinkAsync(
            long   orderCode,
            int    amount,
            string description,
            string returnUrl,
            string cancelUrl);

        bool VerifyWebhookSignature(Dictionary<string, object?> data, string signature);
    }
}
