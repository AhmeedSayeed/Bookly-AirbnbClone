using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
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
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(id, userId);

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
    }
}