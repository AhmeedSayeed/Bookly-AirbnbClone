using AutoMapper;
using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Bookings;
using DAL.Enums;
using DAL.Models.Property;
using DAL.Models.Reservations;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<Booking> _bookingRepo;
        private readonly IRepository<Listing> _listingRepo;
        private readonly IMapper _mapper;

        public BookingService(IRepository<Booking> bookingRepo, IRepository<Listing> listingRepo, IMapper mapper)
        {
            _bookingRepo = bookingRepo;
            _listingRepo = listingRepo;
            _mapper = mapper;
        }

        public async Task<Response<int>> RequestBookingAsync(int guestId, BookingRequestViewModel request)
        {
            if (request.CheckInDate.Date < DateTime.UtcNow.Date)
                return Response<int>.Fail(ResponseStatus.ValidationError, "Check-in date cannot be in the past.");

            if (request.CheckOutDate <= request.CheckInDate)
                return Response<int>.Fail(ResponseStatus.ValidationError, "Check-out date must be after check-in date.");

            var listing = await _listingRepo.GetByIdAsync(request.ListingId);
            if (listing == null || !listing.IsActive)
                return Response<int>.Fail(ResponseStatus.NotFound, "Listing is unavailable.");

            if (request.NumberOfGuests > listing.MaxGuests)
                return Response<int>.Fail(ResponseStatus.ValidationError, $"This listing allows a maximum of {listing.MaxGuests} guests.");

            if (listing.HostId == guestId)
                return Response<int>.Fail(ResponseStatus.Forbidden, "You cannot book your own listing.");

            var overlappingBookings = await _bookingRepo.GetAllAsIQueryable()
                .Where(b => b.ListingId == request.ListingId &&
                            b.Status != BookingStatus.Cancelled &&
                            b.Status != BookingStatus.Rejected)
                .AnyAsync(b => request.CheckInDate < b.CheckOutDate && request.CheckOutDate > b.CheckInDate);

            if (overlappingBookings)
                return Response<int>.Fail(ResponseStatus.Error, "Those dates are already booked. Please choose different dates.");

            var hasBlockedDates = await _listingRepo.GetAllAsIQueryable()
                .Where(l => l.Id == request.ListingId)
                .AnyAsync(l => l.BlockedDates.Any(bd => bd.Date >= request.CheckInDate.Date && bd.Date < request.CheckOutDate.Date));

            if (hasBlockedDates)
                return Response<int>.Fail(ResponseStatus.ValidationError, "The selected date range contains dates blocked by the host.");

            int totalNights = (request.CheckOutDate.Date - request.CheckInDate.Date).Days;

            var booking = _mapper.Map<Booking>(request);
            booking.GuestId = guestId;
            booking.TotalPrice = totalNights * listing.PricePerNight;

            await _bookingRepo.AddAsync(booking);
            var saved = await _bookingRepo.SaveAsync();

            if (saved > 0)
                return Response<int>.Success(booking.Id, "Booking request sent to the host successfully.");

            return Response<int>.Fail(ResponseStatus.Error, "A system error occurred while processing your request.");
        }

        public async Task<Response<BookingConfirmationViewModel>> GetBookingConfirmationAsync(int bookingId, int guestId)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId);

            if (booking == null)
                return Response<BookingConfirmationViewModel>.Fail(ResponseStatus.NotFound, "Booking not found.");

            var viewModel = _mapper.Map<BookingConfirmationViewModel>(booking);
            return Response<BookingConfirmationViewModel>.Success(viewModel);
        }

        public async Task<Response<MyTripsViewModel>> GetMyTripsAsync(int guestId)
        {
            var bookings = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                    .ThenInclude(l => l.Photos)
                .Include(b => b.Review)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var viewModel = new MyTripsViewModel
            {
                Upcoming = bookings.Where(b => b.CheckInDate >= DateTime.UtcNow.Date &&
                                               b.Status != BookingStatus.Cancelled &&
                                               b.Status != BookingStatus.Rejected &&
                                               b.Status != BookingStatus.Completed)
                                   .Select(b => _mapper.Map<BookingCardViewModel>(b)).ToList(),

                Past = bookings.Where(b => b.CheckInDate < DateTime.UtcNow.Date ||
                                           b.Status == BookingStatus.Completed ||
                                           b.Status == BookingStatus.Cancelled ||
                                           b.Status == BookingStatus.Rejected)
                               .Select(b => _mapper.Map<BookingCardViewModel>(b)).ToList()
            };

            return Response<MyTripsViewModel>.Success(viewModel);
        }

        public async Task<Response<HostBookingsViewModel>> GetHostBookingsAsync(int hostId)
        {
            var requests = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .Include(b => b.Guest)
                .Where(b => b.Listing.HostId == hostId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var viewModel = new HostBookingsViewModel
            {
                Requests = requests.Select(b => _mapper.Map<BookingRequestCardViewModel>(b)).ToList()
            };

            return Response<HostBookingsViewModel>.Success(viewModel);
        }

        public async Task<Response<BookingDetailsViewModel>> GetBookingDetailsAsync(int bookingId, int userId)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                    .ThenInclude(l => l.Host)
                .Include(b => b.Listing)
                    .ThenInclude(l => l.Photos)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return Response<BookingDetailsViewModel>.Fail(ResponseStatus.NotFound, "Booking not found.");

            // Security check: Only the Host or the Guest can view these details
            if (booking.GuestId != userId && booking.Listing.HostId != userId)
                return Response<BookingDetailsViewModel>.Fail(ResponseStatus.Forbidden, "You do not have permission to view this booking.");

            var viewModel = _mapper.Map<BookingDetailsViewModel>(booking);
            return Response<BookingDetailsViewModel>.Success(viewModel);
        }

        public async Task<Response<bool>> RespondToBookingRequestAsync(int bookingId, int hostId, bool accept)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return Response<bool>.Fail(ResponseStatus.NotFound, "Booking not found.");

            if (booking.Listing.HostId != hostId)
                return Response<bool>.Fail(ResponseStatus.Forbidden, "You do not own this listing.");

            if (booking.Status != BookingStatus.Pending)
                return Response<bool>.Fail(ResponseStatus.ValidationError, "This booking is no longer pending.");

            booking.Status = accept ? BookingStatus.Confirmed : BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;

            _bookingRepo.Update(booking);
            var saved = await _bookingRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.Success(true, accept ? "Booking confirmed successfully." : "Booking rejected.")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to update booking status.");
        }

        public async Task<Response<bool>> CancelBookingAsync(int bookingId, int userId)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return Response<bool>.Fail(ResponseStatus.NotFound, "Booking not found.");

            if (booking.GuestId != userId && booking.Listing.HostId != userId)
                return Response<bool>.Fail(ResponseStatus.Forbidden, "You do not have permission to cancel this booking.");

            if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
                return Response<bool>.Fail(ResponseStatus.ValidationError, "This booking cannot be cancelled.");

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            _bookingRepo.Update(booking);
            var saved = await _bookingRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.Success(true, "Booking cancelled successfully.")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to cancel booking.");
        }

        public async Task<Response<int>> AutoCompletePastBookingsAsync()
        {
            var pastBookings = await _bookingRepo.GetAllAsIQueryable()
                .Where(b => b.CheckOutDate < DateTime.UtcNow.Date &&
                           (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid))
                .ToListAsync();

            if (!pastBookings.Any())
                return Response<int>.Success(0, "No bookings to complete.");

            foreach (var booking in pastBookings)
            {
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;
                _bookingRepo.Update(booking);
            }

            var saved = await _bookingRepo.SaveAsync();
            return Response<int>.Success(saved, $"{saved} bookings marked as completed.");
        }
    }
}