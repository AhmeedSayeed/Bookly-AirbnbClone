using BLL.ViewModels.Common;

namespace BLL.ViewModels.Home;

public class HomeViewModel
{
    public List<ListingCardViewModel> FeaturedListings { get; set; } = new();
    public SearchFilterViewModel SearchFilters { get; set; } = new();
}
