using BLL.Services.Interfaces;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ChatController(
            IChatService chatService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer)
        {
            _chatService = chatService;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: /Chat/Inbox
        public async Task<IActionResult> Inbox(int? conversationId = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Challenge();

            var response = await _chatService.GetUserInboxAsync(userId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = !string.IsNullOrWhiteSpace(response.MessageKey)
                    ? _localizer[response.MessageKey].Value
                    : response.Message;

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ActiveConversationId = conversationId;
            ViewBag.CurrentUserId = userId;

            return View(response.Data);
        }

        // GET: /Chat/GetMessages?conversationId={id}
        [HttpGet]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var response = await _chatService.GetConversationMessagesAsync(conversationId, userId);

            if (!response.Succeeded)
                return BadRequest(new
                {
                    success = false,
                    message = !string.IsNullOrWhiteSpace(response.MessageKey)
                        ? _localizer[response.MessageKey].Value
                        : response.Message
                });

            await _chatService.MarkConversationAsReadAsync(conversationId, userId);

            return Json(new
            {
                success = true,
                messages = response.Data,
                currentUserId = userId
            });
        }

        // POST: /Chat/StartConversation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConversation(int listingId, int hostId)
        {
            var guestIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(guestIdStr, out int guestId))
                return Challenge();

            var response = await _chatService.GetOrCreateConversationAsync(listingId, guestId, hostId);

            if (!response.Succeeded)
            {
                TempData["ErrorMessage"] = !string.IsNullOrWhiteSpace(response.MessageKey)
                    ? _localizer[response.MessageKey].Value
                    : response.Message;

                return RedirectToAction("Details", "Listings", new { id = listingId });
            }

            return RedirectToAction(nameof(Inbox), new { conversationId = response.Data.Id });
        }
    }
}