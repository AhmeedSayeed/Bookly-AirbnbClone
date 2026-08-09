using System;
using DAL.Enums;

namespace DAL.Models.Reservations
{
    public class Payment
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int BookingId { get; set; }
        
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? PaymobOrderId { get; set; }
        public string? PaymobTransactionId { get; set; }
        public int IntegrationId { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Property
        public Booking Booking { get; set; } = null!;
    }
}