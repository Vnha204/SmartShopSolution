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
        private readonly IRazorViewRenderer _viewRenderer;

        private const string AdminEmail = "dov220904@gmail.com";

        public ContactController(IUnitOfWork unitOfWork, EmailService emailService, IRazorViewRenderer viewRenderer)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _viewRenderer = viewRenderer;
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
                var sentAt = DateTime.UtcNow;

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

                var customerBody = await _viewRenderer.RenderAsync(
                    "~/Views/Emails/CustomerContactConfirmation.cshtml",
                    new CustomerEmailViewModel
                    {
                        FullName = model.FullName,
                        Phone    = model.Phone,
                        Message  = model.Message
                    });

                await _emailService.SendAsync(
                    model.Email,
                    model.FullName,
                    "Cảm ơn bạn đã liên hệ với chúng tôi!",
                    customerBody);

                var adminBody = await _viewRenderer.RenderAsync(
                    "~/Views/Emails/AdminContactNotification.cshtml",
                    new AdminEmailViewModel
                    {
                        FullName = model.FullName,
                        Phone    = model.Phone,
                        Email    = model.Email,
                        Message  = model.Message,
                        SentAt   = sentAt
                    });

                await _emailService.SendAsync(
                    AdminEmail,
                    "Admin SmartShop",
                    $"Liên hệ mới từ {model.FullName} - {sentAt:dd/MM/yyyy HH:mm}",
                    adminBody);

                return Json(new { success = true, message = "Gửi thành công! Chúng tôi sẽ phản hồi sớm nhất." });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại sau." });
            }
        }
    }
}
