using BLL.ViewModels.Common;

namespace BLL.ViewModels.Listings;

public class SearchResultsViewModel
{
    public List<ListingCardViewModel> Results { get; set; } = new();
    public SearchFilterViewModel Filters { get; set; } = new();
    public PageInfoViewModel PageInfo { get; set; } = new();
}
