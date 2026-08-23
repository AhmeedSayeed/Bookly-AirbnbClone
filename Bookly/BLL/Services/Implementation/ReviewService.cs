using AutoMapper;
using BLL.DTOs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Reviews;
using DAL.Enums;
using DAL.Models.Interactions;
using DAL.Models.Reservations;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Review> _reviewRepo;
        private readonly IRepository<HostResponse> _hostResponseRepo;
        private readonly IRepository<Booking> _bookingRepo;
        private readonly IMapper _mapper;

        public ReviewService(
            IRepository<Review> reviewRepo,
            IRepository<HostResponse> hostResponseRepo,
            IRepository<Booking> bookingRepo,
            IMapper mapper)
        {
            _reviewRepo = reviewRepo;
            _hostResponseRepo = hostResponseRepo;
            _bookingRepo = bookingRepo;
            _mapper = mapper;
        }

        public async Task<Response<CreateReviewViewModel>> GetReviewFormAsync(int bookingId, int guestId)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Listing)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return Response<CreateReviewViewModel>.Fail(ResponseStatus.NotFound, "Booking not found.");

            if (booking.GuestId != guestId)
                return Response<CreateReviewViewModel>.Fail(ResponseStatus.Forbidden, "This is not your booking.");

            if (booking.Status != BookingStatus.Completed)
                return Response<CreateReviewViewModel>.Fail(ResponseStatus.ValidationError, "You can only review a stay after it's completed.");

            if (booking.Review != null)
                return Response<CreateReviewViewModel>.Fail(ResponseStatus.Conflict, "You have already reviewed this stay.");

            var model = new CreateReviewViewModel
            {
                BookingId = booking.Id,
                ListingTitle = booking.Listing.Title
            };

            return Response<CreateReviewViewModel>.Success(model);
        }

        public async Task<Response<bool>> SubmitReviewAsync(int guestId, CreateReviewViewModel model)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
                return Response<bool>.Fail(ResponseStatus.NotFound, "Booking not found.");

            if (booking.GuestId != guestId)
                return Response<bool>.Fail(ResponseStatus.Forbidden, "This is not your booking.");

            if (booking.Status != BookingStatus.Completed)
                return Response<bool>.Fail(ResponseStatus.ValidationError, "You can only review a stay after it's completed.");

            if (booking.Review != null)
                return Response<bool>.Fail(ResponseStatus.Conflict, "You have already reviewed this stay.");

            var review = _mapper.Map<Review>(model);
            review.BookingId = booking.Id;

            await _reviewRepo.AddAsync(review);
            var saved = await _reviewRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.Success(true, "Thanks for sharing your review!")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to submit review.");
        }

        public async Task<Response<HostResponseViewModel>> GetRespondFormAsync(int reviewId, int hostId)
        {
            var review = await _reviewRepo.GetAllAsIQueryable()
                .Include(r => r.Booking).ThenInclude(b => b.Listing)
                .Include(r => r.Booking).ThenInclude(b => b.Guest)
                .Include(r => r.HostResponse)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return Response<HostResponseViewModel>.Fail(ResponseStatus.NotFound, "Review not found.");

            if (review.Booking.Listing.HostId != hostId)
                return Response<HostResponseViewModel>.Fail(ResponseStatus.Forbidden, "This review isn't on one of your listings.");

            if (review.HostResponse != null)
                return Response<HostResponseViewModel>.Fail(ResponseStatus.Conflict, "You've already responded to this review.");

            var model = new HostResponseViewModel
            {
                ReviewId = review.Id,
                Review = _mapper.Map<ReviewViewModel>(review)
            };

            return Response<HostResponseViewModel>.Success(model);
        }

        public async Task<Response<bool>> RespondToReviewAsync(int hostId, HostResponseViewModel model)
        {
            var review = await _reviewRepo.GetAllAsIQueryable()
                .Include(r => r.Booking).ThenInclude(b => b.Listing)
                .Include(r => r.HostResponse)
                .FirstOrDefaultAsync(r => r.Id == model.ReviewId);

            if (review == null)
                return Response<bool>.Fail(ResponseStatus.NotFound, "Review not found.");

            if (review.Booking.Listing.HostId != hostId)
                return Response<bool>.Fail(ResponseStatus.Forbidden, "This review isn't on one of your listings.");

            if (review.HostResponse != null)
                return Response<bool>.Fail(ResponseStatus.Conflict, "You've already responded to this review.");

            var hostResponse = new HostResponse
            {
                ReviewId = review.Id,
                Content = model.ResponseText,
                RespondedAt = DateTime.UtcNow
            };

            await _hostResponseRepo.AddAsync(hostResponse);
            var saved = await _hostResponseRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.Success(true, "Your response has been posted.")
                : Response<bool>.Fail(ResponseStatus.Error, "Failed to post response.");
        }
    }
}