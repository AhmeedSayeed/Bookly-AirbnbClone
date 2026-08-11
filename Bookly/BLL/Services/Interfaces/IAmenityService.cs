using BLL.DTOs;
using BLL.DTOs.Amenity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IAmenityService
    {
        Task<Response<IEnumerable<AmenityDto>>> GetAllAsync();
    }
}