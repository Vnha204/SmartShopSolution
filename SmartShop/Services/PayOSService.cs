using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartShop.Services
{
    public class PayOSSettings
    {
        public string ClientId    { get; set; } = string.Empty;
        public string ApiKey      { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
    }

    public class PayOSService : IPayOSService
    {
        private readonly IHttpClientFactory    _httpClientFactory;
        private readonly PayOSSettings         _settings;
        private readonly ILogger<PayOSService> _logger;

        private const string ApiBaseUrl = "https://api-merchant.payos.vn";

        public PayOSService(
            IHttpClientFactory      httpClientFactory,
            IOptions<PayOSSettings> options,
            ILogger<PayOSService>   logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings          = options.Value;
            _logger            = logger;
        }

        public async Task<PayOSPaymentResult> CreatePaymentLinkAsync(
            long   orderCode,
            int    amount,
            string description,
            string returnUrl,
            string cancelUrl)
        {
            // PayOS signature: (spec: amount, cancelUrl, description, orderCode, returnUrl)
            var sigData   = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            var signature = ComputeHmacSha256(sigData, _settings.ChecksumKey);

            _logger.LogInformation(
                "PayOS CreatePaymentLink — orderCode={OrderCode} amount={Amount} description={Desc} returnUrl={Return} cancelUrl={Cancel}",
                orderCode, amount, description, returnUrl, cancelUrl);

            var payload = new
            {
                orderCode,
                amount,
                description,
                returnUrl,
                cancelUrl,
                signature
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{ApiBaseUrl}/v2/payment-requests")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("x-client-id", _settings.ClientId);
            request.Headers.Add("x-api-key",    _settings.ApiKey);

            var client = _httpClientFactory.CreateClient("PayOS");
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS HTTP error for orderCode {OrderCode}", orderCode);
                return new PayOSPaymentResult { Success = false, ErrorMessage = "Lỗi kết nối đến cổng thanh toán." };
            }

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS {Status} for orderCode {OrderCode}: {Body}",
                    (int)response.StatusCode, orderCode, body);
                return new PayOSPaymentResult { Success = false, ErrorMessage = "Cổng thanh toán từ chối yêu cầu." };
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root      = doc.RootElement;
                var code      = root.GetProperty("code").GetString();

                if (code != "00")
                {
                    var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : null;
                    _logger.LogError("PayOS code {Code} ({Desc}) for orderCode {OrderCode}", code, desc, orderCode);
                    return new PayOSPaymentResult { Success = false, ErrorMessage = desc ?? "Lỗi từ cổng thanh toán." };
                }

                var data2          = root.GetProperty("data");
                var checkoutUrl    = data2.GetProperty("checkoutUrl").GetString();
                var paymentLinkId  = data2.TryGetProperty("paymentLinkId", out var plEl) ? plEl.GetString() : null;
                long? ocLong       = data2.TryGetProperty("orderCode", out var ocEl) && ocEl.TryGetInt64(out var oc) ? oc : null;

                return new PayOSPaymentResult
                {
                    Success       = true,
                    CheckoutUrl   = checkoutUrl,
                    PaymentLinkId = paymentLinkId,
                    OrderCode     = ocLong
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PayOS response for orderCode {OrderCode}: {Body}", orderCode, body);
                return new PayOSPaymentResult { Success = false, ErrorMessage = "Không thể xử lý phản hồi từ cổng thanh toán." };
            }
        }

        public bool VerifyWebhookSignature(Dictionary<string, object?> data, string signature)
        {
            // Cách PayOS JS SDK tạo chuỗi dữ liệu:
            //   Object.keys(data).sort().map(k => `${k}=${data[k]}`).join('&')
            // Trong JavaScript:
            // - null → "null"
            // - true → "true" (chữ thường)
            // - number → chuỗi số thập phân
            // Cần xử lý giống hệt để tạo chữ ký/hash chính xác.
            // Giá trị null vẫn phải được đưa vào chuỗi dưới dạng "null".
            var parts = data
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv =>
                {
                    var val = kv.Value switch
                    {
                        null         => "null",
                        bool b       => b ? "true" : "false", 
                        _            => kv.Value.ToString()!
                    };
                    return $"{kv.Key}={val}";
                });

            var dataStr  = string.Join("&", parts);
            var expected = ComputeHmacSha256(dataStr, _settings.ChecksumKey);

            _logger.LogDebug("PayOS webhook — sig dataStr={DataStr}", dataStr);

            return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
