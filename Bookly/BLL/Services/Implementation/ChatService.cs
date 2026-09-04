using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Chat;
using DAL.Models;
using DAL.Models.Chat;
using DAL.Models.Identity;
using DAL.Models.Interactions;
using DAL.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class ChatService : IChatService
    {
        private readonly IRepository<Conversation> _conversationRepo;
        private readonly IRepository<ChatMessage> _messageRepo;
        private readonly IRepository<Notification> _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatService(
            IRepository<Conversation> conversationRepo,
            IRepository<ChatMessage> messageRepo,
            IRepository<Notification> notificationRepo,
            UserManager<ApplicationUser> userManager)
        {
            _conversationRepo = conversationRepo;
            _messageRepo = messageRepo;
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }

        public async Task<Response<List<ConversationViewModel>>> GetUserInboxAsync(int userId)
        {
            var conversations = await _conversationRepo.GetAllAsIQueryable()
                .Include(c => c.Listing)
                    .ThenInclude(l => l.Photos)
                .Include(c => c.Guest)
                .Include(c => c.Host)
                .Include(c => c.Messages)
                .Where(c => c.GuestId == userId || c.HostId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var viewModels = conversations.Select(c =>
            {
                var isCurrentUserGuest = c.GuestId == userId;
                var otherUser = isCurrentUserGuest ? c.Host : c.Guest;
                var lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

                return new ConversationViewModel
                {
                    Id = c.Id,
                    ListingId = c.ListingId,
                    ListingTitle = c.Listing?.Title ?? "Unknown Listing",
                    ListingPhotoUrl = c.Listing?.Photos?.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.Url,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.FirstName + " " + otherUser.LastName,
                    OtherUserPhotoUrl = otherUser.ProfilePhotoUrl,
                    LastMessageContent = lastMessage?.Content ?? "Started a conversation",
                    LastMessageAt = c.LastMessageAt,
                    UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId)
                };
            }).ToList();

            return Response<List<ConversationViewModel>>.Success(viewModels);
        }

        public async Task<Response<List<MessageViewModel>>> GetConversationMessagesAsync(int conversationId, int currentUserId)
        {
            var conversation = await _conversationRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Id == conversationId && (c.GuestId == currentUserId || c.HostId == currentUserId));

            if (conversation == null)
                return Response<List<MessageViewModel>>.FailWithKey(ResponseStatus.Forbidden, "AccessDenied");

            var messages = await _messageRepo.GetAllAsIQueryable()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            return Response<List<MessageViewModel>>.Success(messages);
        }

        public async Task<Response<ConversationViewModel>> GetOrCreateConversationAsync(int listingId, int guestId, int hostId)
        {
            if (guestId == hostId)
                return Response<ConversationViewModel>.FailWithKey(ResponseStatus.ValidationError, "CannotMessageYourself");

            var conversation = await _conversationRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.ListingId == listingId && c.GuestId == guestId && c.HostId == hostId);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    ListingId = listingId,
                    GuestId = guestId,
                    HostId = hostId,
                    LastMessageAt = DateTime.UtcNow
                };

                await _conversationRepo.AddAsync(conversation);
                await _conversationRepo.SaveAsync();
            }

            var inboxResponse = await GetUserInboxAsync(guestId);
            var viewModel = inboxResponse.Data.FirstOrDefault(c => c.Id == conversation.Id);

            return Response<ConversationViewModel>.Success(viewModel);
        }

        public async Task<Response<MessageViewModel>> SendMessageAsync(int conversationId, int senderId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Response<MessageViewModel>.FailWithKey(ResponseStatus.ValidationError, "MessageCannotBeEmpty");

            var conversation = await _conversationRepo.GetByIdAsync(conversationId);
            if (conversation == null || (conversation.GuestId != senderId && conversation.HostId != senderId))
                return Response<MessageViewModel>.FailWithKey(ResponseStatus.Forbidden, "AccessDenied");

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _messageRepo.AddAsync(message);

            // 1. Update conversation timestamp
            conversation.LastMessageAt = message.CreatedAt;
            _conversationRepo.Update(conversation);

            // 2. Create physical Notification for the Bell/Database
            var receiverId = conversation.GuestId == senderId ? conversation.HostId : conversation.GuestId;
            var sender = await _userManager.FindByIdAsync(senderId.ToString());
            var senderName = sender?.FirstName ?? "User";

            var notification = new Notification
            {
                UserId = receiverId,
                MessageKey = "NewMessageFrom",
                MessageArgsJson = JsonSerializer.Serialize(new[]
                {
                    senderName,
                    content
                }),
                Link = $"/Chat/Inbox?conversationId={conversationId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(notification);

            // Save all changes (Message + Notification + Conversation update)
            await _messageRepo.SaveAsync();

            var viewModel = new MessageViewModel
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead
            };

            return Response<MessageViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> MarkConversationAsReadAsync(int conversationId, int userId)
        {
            var unreadMessages = await _messageRepo.GetAllAsIQueryable()
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any())
                return Response<bool>.Success(true);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                _messageRepo.Update(message);
            }

            await _messageRepo.SaveAsync();
            return Response<bool>.Success(true);
        }
    }
}