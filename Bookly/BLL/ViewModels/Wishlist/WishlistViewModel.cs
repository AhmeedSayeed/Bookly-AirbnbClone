using BLL.ViewModels.Common;

namespace BLL.ViewModels.Wishlist;

public class WishlistViewModel
{
    public List<ListingCardViewModel> Listings { get; set; } = new();
}
