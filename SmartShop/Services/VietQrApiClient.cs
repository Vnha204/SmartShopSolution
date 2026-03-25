using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SmartShop.Models;

namespace SmartShop.Services
{
    public class VietQrApiClient : IVietQrApiClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly VietQRSettings     _settings;
        private readonly ILogger<VietQrApiClient> _logger;

        public VietQrApiClient(
            IHttpClientFactory httpFactory,
            IOptions<VietQRSettings> options,
            ILogger<VietQrApiClient> logger)
        {
            _httpFactory = httpFactory;
            _settings    = options.Value;
            _logger      = logger;
        }

        public async Task<string> GetQrDataUrlAsync(
            string bankBin,
            string accountNo,
            string accountName,
            long   amount,
            string addInfo)
        {
            var client = _httpFactory.CreateClient("VietQR");

            var body = new
            {
                acqId       = bankBin,
                accountNo   = accountNo,
                accountName = accountName,
                amount      = amount,
                addInfo     = addInfo,
                format      = "text",
                template    = "compact"
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_settings.ApiBaseUrl.TrimEnd('/')}/v2/generate");

            request.Content = JsonContent.Create(body);

            if (!string.IsNullOrEmpty(_settings.ApiClientId))
                request.Headers.TryAddWithoutValidation("x-client-id", _settings.ApiClientId);
            if (!string.IsNullOrEmpty(_settings.ApiApiKey))
                request.Headers.TryAddWithoutValidation("x-api-key", _settings.ApiApiKey);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VietQR API request failed");
                throw new InvalidOperationException("Không thể kết nối đến VietQR API.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body2 = await response.Content.ReadAsStringAsync();
                _logger.LogError("VietQR API returned {Status}: {Body}", response.StatusCode, body2);
                throw new InvalidOperationException($"VietQR API trả về lỗi {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadFromJsonAsync<VietQrApiResponse>();

            var dataUrl = json?.Data?.QrDataURL;
            if (string.IsNullOrEmpty(dataUrl))
                throw new InvalidOperationException("VietQR API không trả về dữ liệu QR hợp lệ.");

            return dataUrl;
        }

        // ── Private response models ────────────────────────────────────────────

        private sealed class VietQrApiResponse
        {
            public VietQrData? Data { get; set; }
        }

        private sealed class VietQrData
        {
            public string? QrDataURL { get; set; }
        }
    }
}
