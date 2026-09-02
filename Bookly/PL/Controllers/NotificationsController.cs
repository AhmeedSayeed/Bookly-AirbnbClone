using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public NotificationsController(
            INotificationService notificationService,
            IStringLocalizer<SharedResource> localizer)
        {
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdString);
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetForUserAsync(userId, pageNumber, pageSize);

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
        {
            var userId = GetCurrentUserId();

            await _notificationService.MarkAsReadAsync(id, userId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetUnreadCountAsync(userId);

            return Json(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownList()
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetForUserAsync(userId, 1, 10);

            // ترجمة الإشعارات قبل إرسالها للجافاسكريبت
            var notifications = response.Data.Notifications.Select(n =>
            {
                string[] args = null;
                if (!string.IsNullOrEmpty(n.MessageArgsJson))
                {
                    try { args = JsonSerializer.Deserialize<string[]>(n.MessageArgsJson); } catch { }
                }

                var translatedText = !string.IsNullOrWhiteSpace(n.MessageKey)
                    ? (args != null && args.Length > 0
                        ? _localizer[n.MessageKey, args].Value
                        : _localizer[n.MessageKey].Value)
                    : n.LegacyMessage;

                return new
                {
                    id = n.Id,
                    message = translatedText,
                    link = n.Link,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt
                };
            });

            return Json(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsReadAjax(int id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok();
        }
    }
}