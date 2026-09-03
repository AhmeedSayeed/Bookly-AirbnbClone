using System;

namespace BLL.ViewModels.Chat
{
    public class ConversationViewModel
    {
        public int Id { get; set; }
        
        public int ListingId { get; set; }
        public string ListingTitle { get; set; }
        public string ListingPhotoUrl { get; set; }
        
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserPhotoUrl { get; set; }
        
        public string LastMessageContent { get; set; }
        public DateTime LastMessageAt { get; set; }
        
        public int UnreadCount { get; set; }
    }
}