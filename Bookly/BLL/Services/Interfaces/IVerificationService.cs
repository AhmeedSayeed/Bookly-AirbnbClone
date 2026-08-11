using BLL.DTOs;
using BLL.ViewModels.Account;
using DAL.Models.Identity;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IVerificationService
    {
        Task<Response<HostVerification>> GetVerificationByUserIdAsync(int userId);
        Task<Response<bool>> SubmitVerificationAsync(int userId, BecomeAHostViewModel model);
    }
}