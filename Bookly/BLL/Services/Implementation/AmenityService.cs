using BLL.DTOs;
using BLL.DTOs.Amenity;
using BLL.Services.Interfaces;
using DAL.Models.Property;
using DAL.Repository.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class AmenityService : IAmenityService
    {
        private readonly IRepository<Amenity> _amenityRepo;

        public AmenityService(IRepository<Amenity> amenityRepo)
        {
            _amenityRepo = amenityRepo;
        }

        public async Task<Response<IEnumerable<AmenityDto>>> GetAllAsync()
        {
            var amenities = await _amenityRepo.GetAllAsNoTrackedAsync();

            var dtos = amenities.Select(a => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                IconClass = a.IconClass
            }).ToList();

            return Response<IEnumerable<AmenityDto>>.Success(dtos);
        }
    }
}