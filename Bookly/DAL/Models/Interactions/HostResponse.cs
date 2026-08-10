using DAL.Models.Common;
using System;

namespace DAL.Models.Interactions
{
    public class HostResponse : ISoftDeletable
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int ReviewId { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public DateTime RespondedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation Property
        public Review Review { get; set; } = null!;
    }
}