using BLL.Services.Interfaces;
using DAL.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace BLL.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(IChatService chatService, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

            if (int.TryParse(Context.UserIdentifier, out int userId))
            {
                await _chatService.MarkConversationAsReadAsync(conversationId, userId);
            }
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        public async Task SendMessage(int conversationId, int receiverId, string content)
        {
            if (!int.TryParse(Context.UserIdentifier, out int senderId))
                return;

            var response = await _chatService.SendMessageAsync(conversationId, senderId, content);

            if (response.Succeeded)
            {
                var message = response.Data;
                var sender = await _userManager.FindByIdAsync(senderId.ToString());
                var senderName = sender?.FirstName ?? "User";

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);

                var snippet = content.Length > 40 ? content.Substring(0, 40) + "..." : content;

                await Clients.User(receiverId.ToString()).SendAsync("ReceiveChatNotification", new
                {
                    ConversationId = conversationId,
                    SenderName = senderName,
                    MessageSnippet = snippet,
                    CreatedAt = message.CreatedAt
                });
            }
        }
    }
}