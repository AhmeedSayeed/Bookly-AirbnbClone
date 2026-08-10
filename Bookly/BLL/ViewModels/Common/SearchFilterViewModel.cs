namespace BLL.ViewModels.Common;

public class SearchFilterViewModel
{
    public string? City { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Guests { get; set; } = 1;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public bool InstantBookOnly { get; set; }
}
