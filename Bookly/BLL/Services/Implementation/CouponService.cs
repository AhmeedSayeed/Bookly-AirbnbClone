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
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.ValidationError, "Please enter a coupon code.");

            var cleanCode = code.Trim().ToUpper();

            // 1. البحث عن الكوبون
            var coupon = await _couponRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanCode);

            if (coupon == null)
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.NotFound, "Invalid coupon code.");

            // 2. التحقق من تاريخ الصلاحية
            if (coupon.ExpiryDate < DateTime.UtcNow)
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.ValidationError, "This coupon has expired.");

            // 3. التحقق من عدد مرات الاستخدام
            if (coupon.UsesCount >= coupon.MaxUses)
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.ValidationError, "This coupon has reached its maximum usage limit.");

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
                Message = $"Coupon applied successfully! ({coupon.DiscountPercent}% off)"
            };

            return Response<CouponValidationResultDto>.Success(result);
        }

        public async Task<Response<CouponValidationResultDto>> ApplyCouponToBookingAsync(int bookingId, string code, int currentUserId)
        {
            // 1. جلب الحجز والتحقق من صاحبه وحالته
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == currentUserId);

            if (booking == null)
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.NotFound, "Booking not found.");

            if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Pending)
                return Response<CouponValidationResultDto>.Fail(ResponseStatus.ValidationError, "Coupons can only be applied to pending or confirmed bookings.");

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

        public async Task<Response<List<CouponDto>>> GetAllCouponsAsync()
        {
            var coupons = await _couponRepo.GetAllAsIQueryable()
                .OrderByDescending(c => c.ExpiryDate)
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

        public async Task<Response<int>> CreateCouponAsync(CreateCouponDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
                return Response<int>.Fail(ResponseStatus.ValidationError, "Coupon code is required.");

            if (model.DiscountPercent <= 0 || model.DiscountPercent > 100)
                return Response<int>.Fail(ResponseStatus.ValidationError, "Discount percent must be between 1 and 100.");

            if (model.ExpiryDate <= DateTime.UtcNow)
                return Response<int>.Fail(ResponseStatus.ValidationError, "Expiry date must be in the future.");

            if (model.MaxUses <= 0)
                return Response<int>.Fail(ResponseStatus.ValidationError, "Max uses must be greater than 0.");

            var cleanCode = model.Code.Trim().ToUpper();

            var exists = await _couponRepo.GetAllAsIQueryable()
                .AnyAsync(c => c.Code.ToUpper() == cleanCode);

            if (exists)
                return Response<int>.Fail(ResponseStatus.Conflict, "A coupon with this code already exists.");

            var coupon = new Coupon
            {
                Code = cleanCode,
                DiscountPercent = model.DiscountPercent,
                ExpiryDate = model.ExpiryDate,
                MaxUses = model.MaxUses,
                UsesCount = 0,
                IsDeleted = false
            };

            await _couponRepo.AddAsync(coupon);
            var saved = await _couponRepo.SaveAsync();

            return saved > 0
                ? Response<int>.Success(coupon.Id, "Coupon created successfully.")
                : Response<int>.Fail(ResponseStatus.Error, "Failed to create coupon.");
        }

        public async Task<Response<bool>> DeleteCouponAsync(int id)
        {
            var coupon = await _couponRepo.GetByIdAsync(id);
            if (coupon == null)
                return Response<bool>.Fail(ResponseStatus.NotFound, "Coupon not found.");

            _couponRepo.Delete(id);
            var saved = await _couponRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.Success(true, "Coupon deleted successfully.")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to delete coupon.");
        }
    }
}
