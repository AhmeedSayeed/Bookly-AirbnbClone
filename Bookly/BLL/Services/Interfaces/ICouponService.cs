using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface ICouponService
    {
        // Check copoun validation and calculate discount
        Task<Response<CouponValidationResultDto>> ValidateCouponAsync(string code, decimal amount);

        // Apply copoun, edit reservation price, and increase usage count
        Task<Response<CouponValidationResultDto>> ApplyCouponToBookingAsync(int bookingId, string code, int currentUserId);

        // to see active copouns
        Task<Response<List<CouponDto>>> GetActiveCouponsAsync();
    }
}
