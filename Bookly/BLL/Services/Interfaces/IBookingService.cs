using BLL.DTOs;
using BLL.ViewModels.Bookings;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IBookingService
    {
        // Core Booking Action
        Task<Response<int>> RequestBookingAsync(int guestId, BookingRequestViewModel request);
        
        // Guest Views
        Task<Response<BookingConfirmationViewModel>> GetBookingConfirmationAsync(int bookingId, int guestId);
        Task<Response<MyTripsViewModel>> GetMyTripsAsync(int guestId);
        
        // Host Views
        Task<Response<HostBookingsViewModel>> GetHostBookingsAsync(int hostId);
        
        // Shared Views & Actions
        Task<Response<BookingDetailsViewModel>> GetBookingDetailsAsync(int bookingId, int userId);
        Task<Response<bool>> RespondToBookingRequestAsync(int bookingId, int hostId, bool accept);
        Task<Response<bool>> CancelBookingAsync(int bookingId, int userId);
        
        // Background Job (Phase 1 auto-completion)
        Task<Response<int>> AutoCompletePastBookingsAsync();
    }
}