using BLL.DTOs;
using BLL.ViewModels.Reviews;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IReviewService
    {
        // GET /Reviews/Create/{bookingId} - prefills the form with the listing title
        Task<Response<CreateReviewViewModel>> GetReviewFormAsync(int bookingId, int guestId);

        // POST /Reviews/Create - guest submits a rating + optional comment for a completed stay
        Task<Response<bool>> SubmitReviewAsync(int guestId, CreateReviewViewModel model);

        // GET /Reviews/Respond/{reviewId} - prefills the form with the review being responded to
        Task<Response<HostResponseViewModel>> GetRespondFormAsync(int reviewId, int hostId);

        // POST /Reviews/Respond - host posts their one public reply to a review
        Task<Response<bool>> RespondToReviewAsync(int hostId, HostResponseViewModel model);
    }
}