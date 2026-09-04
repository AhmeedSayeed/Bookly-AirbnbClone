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

        public CouponService(
            IRepository<Coupon> couponRepo,
            IRepository<Booking> bookingRepo)
        {
            _couponRepo = couponRepo;
            _bookingRepo = bookingRepo;
        }

        public async Task<Response<CouponValidationResultDto>> ValidateCouponAsync(
            string code,
            decimal amount)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "PleaseEnterCouponCode");
            }

            var cleanCode = code.Trim().ToUpper();

            var coupon = await _couponRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanCode);

            if (coupon == null)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.NotFound,
                    "InvalidCouponCode");
            }

            if (coupon.ExpiryDate < DateTime.UtcNow)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponExpired");
            }

            if (coupon.UsesCount >= coupon.MaxUses)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponMaxUsageReached");
            }

            var discountAmount =
                Math.Round(amount * (coupon.DiscountPercent / 100m), 2);

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

        public async Task<Response<CouponValidationResultDto>> ApplyCouponToBookingAsync(
            int bookingId,
            string code,
            int currentUserId)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(
                    b => b.Id == bookingId && b.GuestId == currentUserId);

            if (booking == null)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.NotFound,
                    "BookingNotFound");
            }

            if (booking.Status != BookingStatus.Confirmed &&
                booking.Status != BookingStatus.Pending)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponsOnlyForPendingOrConfirmed");
            }

            int totalNights = Math.Max(
                1,
                (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);

            decimal originalBasePrice =
                booking.Listing != null
                    ? totalNights * booking.Listing.PricePerNight
                    : booking.TotalPrice;

            if (booking.TotalPrice < originalBasePrice)
            {
                return Response<CouponValidationResultDto>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponAlreadyApplied");
            }

            var validation =
                await ValidateCouponAsync(code, originalBasePrice);

            if (!validation.Succeeded || validation.Data == null)
                return validation;

            var coupon = await _couponRepo.GetAllAsIQueryable()
                .FirstOrDefaultAsync(
                    c => c.Code.ToUpper() == code.Trim().ToUpper());

            if (coupon != null)
            {
                coupon.UsesCount++;
                _couponRepo.Update(coupon);
            }

            booking.TotalPrice = validation.Data.NewTotalPrice;
            booking.UpdatedAt = DateTime.UtcNow;
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
            {
                return Response<int>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponCodeRequired");
            }

            if (model.DiscountPercent <= 0 || model.DiscountPercent > 100)
            {
                return Response<int>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponDiscountPercentInvalid");
            }

            if (model.ExpiryDate <= DateTime.UtcNow)
            {
                return Response<int>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponExpiryDateInvalid");
            }

            if (model.MaxUses <= 0)
            {
                return Response<int>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CouponMaxUsesInvalid");
            }

            var cleanCode = model.Code.Trim().ToUpper();

            var exists = await _couponRepo.GetAllAsIQueryable()
                .AnyAsync(c => c.Code.ToUpper() == cleanCode);

            if (exists)
            {
                return Response<int>.FailWithKey(
                    ResponseStatus.Conflict,
                    "CouponCodeAlreadyExists");
            }

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
                ? Response<int>.SuccessWithKey(
                    coupon.Id,
                    "CouponCreatedSuccessfully")
                : Response<int>.FailWithKey(
                    ResponseStatus.Error,
                    "CouponCreationFailed");
        }

        public async Task<Response<bool>> DeleteCouponAsync(int id)
        {
            var coupon = await _couponRepo.GetByIdAsync(id);

            if (coupon == null)
            {
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "CouponNotFound");
            }

            _couponRepo.Delete(id);
            var saved = await _couponRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.SuccessWithKey(
                    true,
                    "CouponDeletedSuccessfully")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "CouponDeletionFailed");
        }
    }
}