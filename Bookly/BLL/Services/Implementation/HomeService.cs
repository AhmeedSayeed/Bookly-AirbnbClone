using AutoMapper;
using BLL.DTOs;
using BLL.Interfaces;
using BLL.ViewModels.Common;
using BLL.ViewModels.Home;
using BLL.ViewModels.Listings;
using DAL.Models;
using DAL.Models.Property;
using DAL.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class HomeService : IHomeService
    {
        private readonly IRepository<Listing> _listingRepo;
        private readonly IMapper _mapper;

        public HomeService(IRepository<Listing> listingRepo, IMapper mapper)
        {
            _listingRepo = listingRepo;
            _mapper = mapper;
        }

        public async Task<Response<HomeViewModel>> GetHomeDataAsync()
        {
            var pagedResult = await _listingRepo.GetAllPaginatedEnhancedAsync<Listing>(
                selector: l => l,
                pageNumber: 1,
                pageSize: 8,
                filter: l => l.IsActive,
                expandable: false,
                orderBy: q => q.OrderByDescending(l => l.CreatedAt),
                include: q => q.Include(l => l.Photos)
                               .Include(l => l.Bookings)
                               .ThenInclude(b => b.Review)
            );

            var featuredListingsVm = _mapper.Map<List<ListingCardViewModel>>(pagedResult.Items);

            var viewModel = new HomeViewModel
            {
                FeaturedListings = featuredListingsVm
            };

            return Response<HomeViewModel>.Success(viewModel);
        }
    }
}