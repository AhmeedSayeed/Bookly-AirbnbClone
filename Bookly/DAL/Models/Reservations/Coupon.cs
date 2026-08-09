using System;

namespace DAL.Models.Reservations
{
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int MaxUses { get; set; }
        public int UsesCount { get; set; }
    }
}