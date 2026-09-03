using BLL.DTOs;
using BLL.ViewModels.Chat;
using DAL.Models.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IChatService
    {
        Task<Response<List<ConversationViewModel>>> GetUserInboxAsync(int userId);

        Task<Response<List<MessageViewModel>>> GetConversationMessagesAsync(int conversationId, int currentUserId);

        Task<Response<ConversationViewModel>> GetOrCreateConversationAsync(int listingId, int guestId, int hostId);

        Task<Response<MessageViewModel>> SendMessageAsync(int conversationId, int senderId, string content);

        Task<Response<bool>> MarkConversationAsReadAsync(int conversationId, int userId);
    }
}