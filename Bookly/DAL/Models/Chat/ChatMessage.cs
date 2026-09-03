using DAL.Models.Identity;
using System;

namespace DAL.Models.Chat
{
    public class ChatMessage
    {
        public int Id { get; set; }

        // The conversation this message belongs to
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        // The user who sent this specific message (can be Host or Guest)
        public int SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        // The actual text content
        public string Content { get; set; }

        // Used for notification badges and read receipts
        public bool IsRead { get; set; } = false;

        // Timestamp of when the message was sent
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}