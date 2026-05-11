using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Services;
using System.Text.Json;

namespace SmartShop.Controllers
{
    [ApiController]
    [Route("api/payos")]
    public class PayOSWebhookController : ControllerBase
    {
        private readonly IPayOSService _payOS;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PayOSWebhookController> _logger;

        public PayOSWebhookController(
            IPayOSService payOS,
            IUnitOfWork unitOfWork,
            ILogger<PayOSWebhookController> logger)
        {
            _payOS      = payOS;
            _unitOfWork = unitOfWork;
            _logger     = logger;
        }

        //api/payos/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("data",      out var dataEl) ||
                !body.TryGetProperty("signature", out var sigEl))
                return BadRequest(new { error = "Missing required fields" });

            var signature = sigEl.GetString();
            if (string.IsNullOrEmpty(signature))
                return BadRequest(new { error = "Missing signature" });

            var dataDict = new Dictionary<string, object?>();
            foreach (var prop in dataEl.EnumerateObject())
            {
                dataDict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String  => prop.Value.GetString(),
                    JsonValueKind.Number  => prop.Value.TryGetInt64(out var l) ? (object)l : prop.Value.GetDecimal(),
                    JsonValueKind.True    => (object)true,
                    JsonValueKind.False   => (object)false,
                    JsonValueKind.Null    => null,
                    _                    => prop.Value.GetRawText()
                };
            }

            if (!_payOS.VerifyWebhookSignature(dataDict, signature))
            {
                _logger.LogWarning("PayOS webhook: invalid signature");
                return Unauthorized(new { error = "Invalid signature" });
            }
            var code = dataEl.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
            if (!string.Equals(code, "00", StringComparison.Ordinal))
            {
                _logger.LogInformation("PayOS webhook non-success code {Code} — acknowledged", code);
                return Ok(new { message = "acknowledged" });
            }

            var paymentLinkId = dataEl.TryGetProperty("paymentLinkId", out var plEl) ? plEl.GetString() : null;
            long? payosOrderCode = dataEl.TryGetProperty("orderCode", out var ocEl) && ocEl.TryGetInt64(out var oc)
                ? oc : null;
            SmartShop.Models.Order? order = null;

            if (!string.IsNullOrEmpty(paymentLinkId))
            {
                order = await _unitOfWork.Orders.FirstOrDefaultAsync(
                    o => o.PayOSPaymentLinkId == paymentLinkId);
            }

            if (order == null && payosOrderCode.HasValue)
            {
                order = await _unitOfWork.Orders.FirstOrDefaultAsync(
                    o => o.PayOSOrderCode == payosOrderCode.Value);
            }

            if (order == null)
            {
                _logger.LogWarning(
                    "PayOS webhook: no order found for paymentLinkId={LinkId} orderCode={OrderCode}",
                    paymentLinkId, payosOrderCode);
                return Ok(new { message = "order not found" });
            }
            if (string.Equals(order.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = "already paid" });

            order.PaymentStatus = "paid";
            order.UpdatedAt     = DateTime.UtcNow;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Order {OrderId} marked paid via PayOS webhook", order.Id);
            return Ok(new { message = "success" });
        }
    }
}
