using System;

namespace DAL.Models.Interactions
{
    public class HostResponse
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int ReviewId { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public DateTime RespondedAt { get; set; }

        // Navigation Property
        public Review Review { get; set; } = null!;
    }
}