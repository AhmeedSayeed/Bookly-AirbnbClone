using BLL.DTOs;
using BLL.Services.Interfaces;
using DAL.Enums;
using DAL.Models.Reservations;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class CouponService : ICouponService
    {
        private readonly IRepository<Coupon> _couponRepo;
        private readonly IRepository<Booking> _bookingRepo;

        public CouponService(IRepository<Coupon> couponRepo, IRepository<Booking> bookingRepo)
        {
            _couponRepo = couponRepo;
            _bookingRepo = bookingRepo;
        }

        public async Task<Response<CouponValidationResultDto>> ValidateCouponAsync(string code, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.ValidationError, "PleaseEnterCouponCode");

            var cleanCode = code.Trim().ToUpper();

            // 1. البحث عن الكوبون
            var coupon = await _couponRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanCode);

            if (coupon == null)
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.NotFound, "InvalidCouponCode");

            // 2. التحقق من تاريخ الصلاحية
            if (coupon.ExpiryDate < DateTime.UtcNow)
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.ValidationError, "CouponExpired");

            // 3. التحقق من عدد مرات الاستخدام
            if (coupon.UsesCount >= coupon.MaxUses)
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.ValidationError, "CouponMaxUsageReached");

            // 4. حساب الخصم
            var discountAmount = Math.Round(amount * (coupon.DiscountPercent / 100m), 2);
            var newPrice = Math.Max(0, amount - discountAmount);

            var result = new CouponValidationResultDto
            {
                IsValid = true,
                Code = coupon.Code,
                DiscountPercent = coupon.DiscountPercent,
                DiscountAmount = discountAmount,
                OriginalPrice = amount,
                NewTotalPrice = newPrice,
                MessageKey = "CouponAppliedSuccessfully",
                MessageArgs = new[] { coupon.DiscountPercent.ToString() }
            };

            return Response<CouponValidationResultDto>.Success(result);
        }

        public async Task<Response<CouponValidationResultDto>> ApplyCouponToBookingAsync(int bookingId, string code, int currentUserId)
        {
            // 1. جلب الحجز والتحقق من صاحبه وحالته
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == currentUserId);

            if (booking == null)
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.NotFound, "BookingNotFound");

            if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Pending)
                return Response<CouponValidationResultDto>.FailWithKey(ResponseStatus.ValidationError, "CouponsOnlyForPendingOrConfirmed");

            // 2. فحص الكوبون
            var validation = await ValidateCouponAsync(code, booking.TotalPrice);
            if (!validation.Succeeded || validation.Data == null)
                return validation;

            // 3. جلب الكوبون لزيادة عداد الاستخدام
            var coupon = await _couponRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.Trim().ToUpper());

            if (coupon != null)
            {
                coupon.UsesCount++;
                _couponRepo.Update(coupon);
            }

            // 4. تحديث سعر الحجز الإجمالي
            booking.TotalPrice = validation.Data.NewTotalPrice;
            _bookingRepo.Update(booking);

            await _bookingRepo.SaveAsync();
            await _couponRepo.SaveAsync();

            return validation;
        }

        public async Task<Response<List<CouponDto>>> GetActiveCouponsAsync()
        {
            var now = DateTime.UtcNow;
            var coupons = await _couponRepo.GetAllAsIQueryable()
                .Where(c => c.ExpiryDate >= now && c.UsesCount < c.MaxUses)
                .Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    DiscountPercent = c.DiscountPercent,
                    ExpiryDate = c.ExpiryDate,
                    MaxUses = c.MaxUses,
                    UsesCount = c.UsesCount
                })
                .ToListAsync();

            return Response<List<CouponDto>>.Success(coupons);
        }
    }
}