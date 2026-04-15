using Microsoft.AspNetCore.Mvc;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Services;

namespace SmartShop.Controllers
{
    public class ContactController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmailService _emailService;

        private const string AdminEmail = "dov220904@gmail.com";

        public ContactController(IUnitOfWork unitOfWork, EmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors[0].ErrorMessage);

                return Json(new { success = false, errors });
            }

            try
            {
                var sentAt = DateTime.Now;

                var contact = new ContactMessage
                {
                    FullName = model.FullName,
                    Phone    = model.Phone,
                    Email    = model.Email,
                    Message  = model.Message,
                    SentAt   = sentAt
                };

                await _unitOfWork.ContactMessages.AddAsync(contact);
                await _unitOfWork.SaveAsync();

                await _emailService.SendAsync(
                    model.Email,
                    model.FullName,
                    "Cảm ơn bạn đã liên hệ với chúng tôi!",
                    BuildCustomerEmailBody(model, sentAt));

                await _emailService.SendAsync(
                    AdminEmail,
                    "Admin SmartShop",
                    $"Liên hệ mới từ {model.FullName} - {sentAt:dd/MM/yyyy HH:mm}",
                    BuildAdminEmailBody(model, sentAt));

                return Json(new { success = true, message = "Gửi thành công! Chúng tôi sẽ phản hồi sớm nhất." });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại sau." });
            }
        }

        private static string BuildCustomerEmailBody(ContactViewModel model, DateTime sentAt)
        {
            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head><meta charset=""utf-8""></head>
<body style=""font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;"">
  <h2>Xin chào {model.FullName},</h2>
  <p>Cảm ơn bạn đã liên hệ với <strong>SmartShop</strong>!</p>
  <p>Chúng tôi đã nhận được tin nhắn của bạn và sẽ phản hồi trong vòng <strong>24 giờ</strong>.</p>
  <h3 style=""margin-top:24px;"">Thông tin bạn đã gửi:</h3>
  <table border=""0"" cellpadding=""8"" cellspacing=""0"" style=""border-collapse:collapse;"">
    <tr><td style=""font-weight:bold;padding-right:16px;"">Họ tên:</td><td>{model.FullName}</td></tr>
    <tr><td style=""font-weight:bold;padding-right:16px;"">Số điện thoại:</td><td>{model.Phone}</td></tr>
    <tr><td style=""font-weight:bold;padding-right:16px;"">Tin nhắn:</td><td>{model.Message}</td></tr>
  </table>
  <p style=""margin-top:32px;"">Trân trọng,<br><strong>SmartShop</strong></p>
</body>
</html>";
        }

        private static string BuildAdminEmailBody(ContactViewModel model, DateTime sentAt)
        {
            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head><meta charset=""utf-8""></head>
<body style=""font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;"">
  <h2>Liên hệ mới từ khách hàng</h2>
  <table border=""1"" cellpadding=""10"" cellspacing=""0"" style=""border-collapse:collapse;width:100%;border-color:#ddd;"">
    <tr style=""background:#f5f5f5;""><td style=""font-weight:bold;width:160px;"">Họ tên</td><td>{model.FullName}</td></tr>
    <tr><td style=""font-weight:bold;"">Số điện thoại</td><td>{model.Phone}</td></tr>
    <tr style=""background:#f5f5f5;""><td style=""font-weight:bold;"">Email</td><td>{model.Email}</td></tr>
    <tr><td style=""font-weight:bold;"">Tin nhắn</td><td>{model.Message}</td></tr>
    <tr style=""background:#f5f5f5;""><td style=""font-weight:bold;"">Thời gian gửi</td><td>{sentAt:dd/MM/yyyy HH:mm:ss}</td></tr>
  </table>
</body>
</html>";
        }
    }
}
