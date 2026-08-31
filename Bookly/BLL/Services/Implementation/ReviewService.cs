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
                return Response<CreateReviewViewModel>.FailWithKey(
                    ResponseStatus.NotFound,
                    "BookingNotFound");

            if (booking.GuestId != guestId)
                return Response<CreateReviewViewModel>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "ThisIsNotYourBooking");

            if (booking.Status != BookingStatus.Completed)
                return Response<CreateReviewViewModel>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CanOnlyReviewCompletedStay");

            if (booking.Review != null)
                return Response<CreateReviewViewModel>.FailWithKey(
                    ResponseStatus.Conflict,
                    "AlreadyReviewedStay");

            var model = new CreateReviewViewModel
            {
                BookingId = booking.Id,
                ListingTitle = booking.Listing.Title
            };

            return Response<CreateReviewViewModel>.Success(model);
        }

        public async Task<Response<bool>> SubmitReviewAsync(
            int guestId,
            CreateReviewViewModel model)
        {
            var booking = await _bookingRepo.GetAllAsIQueryable()
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "BookingNotFound");

            if (booking.GuestId != guestId)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "ThisIsNotYourBooking");

            if (booking.Status != BookingStatus.Completed)
                return Response<bool>.FailWithKey(
                    ResponseStatus.ValidationError,
                    "CanOnlyReviewCompletedStay");

            if (booking.Review != null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Conflict,
                    "AlreadyReviewedStay");

            var review = _mapper.Map<Review>(model);
            review.BookingId = booking.Id;

            await _reviewRepo.AddAsync(review);
            var saved = await _reviewRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.SuccessWithKey(
                    true,
                    "ReviewSubmittedSuccessfully")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "FailedToSubmitReview");
        }

        public async Task<Response<HostResponseViewModel>> GetRespondFormAsync(
            int reviewId,
            int hostId)
        {
            var review = await _reviewRepo.GetAllAsIQueryable()
                .Include(r => r.Booking).ThenInclude(b => b.Listing)
                .Include(r => r.Booking).ThenInclude(b => b.Guest)
                .Include(r => r.HostResponse)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return Response<HostResponseViewModel>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ReviewNotFound");

            if (review.Booking.Listing.HostId != hostId)
                return Response<HostResponseViewModel>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "ReviewNotOnYourListing");

            if (review.HostResponse != null)
                return Response<HostResponseViewModel>.FailWithKey(
                    ResponseStatus.Conflict,
                    "AlreadyRespondedToReview");

            var model = new HostResponseViewModel
            {
                ReviewId = review.Id,
                Review = _mapper.Map<ReviewViewModel>(review)
            };

            return Response<HostResponseViewModel>.Success(model);
        }

        public async Task<Response<bool>> RespondToReviewAsync(
            int hostId,
            HostResponseViewModel model)
        {
            var review = await _reviewRepo.GetAllAsIQueryable()
                .Include(r => r.Booking).ThenInclude(b => b.Listing)
                .Include(r => r.HostResponse)
                .FirstOrDefaultAsync(r => r.Id == model.ReviewId);

            if (review == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "ReviewNotFound");

            if (review.Booking.Listing.HostId != hostId)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Forbidden,
                    "ReviewNotOnYourListing");

            if (review.HostResponse != null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.Conflict,
                    "AlreadyRespondedToReview");

            var hostResponse = new HostResponse
            {
                ReviewId = review.Id,
                Content = model.ResponseText,
                RespondedAt = DateTime.UtcNow
            };

            await _hostResponseRepo.AddAsync(hostResponse);
            var saved = await _hostResponseRepo.SaveAsync();

            return saved > 0
                ? Response<bool>.SuccessWithKey(
                    true,
                    "ReviewResponsePostedSuccessfully")
                : Response<bool>.FailWithKey(
                    ResponseStatus.Error,
                    "FailedToPostResponse");
        }
    }
}