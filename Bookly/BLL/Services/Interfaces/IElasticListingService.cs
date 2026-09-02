using BLL.DTOs.Elasticsearch;
using BLL.DTOs.Listing;
using DAL.Models.Common;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IElasticListingService
    {
        Task<bool> IndexListingAsync(ListingDocument document);
        Task<bool> DeleteListingAsync(int listingId);

        Task<PagedResult<ListingCardDto>> SearchAsync(ListingSearchRequestDto request);
    }
}