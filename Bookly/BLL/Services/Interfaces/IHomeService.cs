using System.Threading.Tasks;
using BLL.DTOs;
using BLL.ViewModels.Home;

namespace BLL.Interfaces
{
    public interface IHomeService
    {
        Task<Response<HomeViewModel>> GetHomeDataAsync(int page = 1, int pageSize = 12);
    }
}