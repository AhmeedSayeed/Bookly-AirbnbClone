using System.Threading.Tasks;
using BLL.DTOs;
using BLL.ViewModels.Home;

namespace BLL.Interfaces
{
    public interface IHomeService
    {
        Task<Response<HomeViewModel>> GetHomeDataAsync();
    }
}