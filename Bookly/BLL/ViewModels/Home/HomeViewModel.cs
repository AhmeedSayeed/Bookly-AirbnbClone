using BLL.ViewModels.Common;
using System.Collections.Generic;

namespace BLL.ViewModels.Home
{
    public class HomeViewModel
    {
        public List<ListingCardViewModel> FeaturedListings { get; set; } = new();
        public SearchFilterViewModel SearchFilters { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}