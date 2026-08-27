using BLL.DTOs;
using DAL.Enums;
using System.Threading.Tasks;

namespace BLL.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Response<string>> InitiatePaymentAsync(int bookingId, int userId, PaymentMethod method);
        Task<Response<bool>> ProcessWebhookAsync(string requestBody);
        Task ConfirmPaymentDirectAsync(int bookingId);
        bool ValidateHmac(Dictionary<string, string> queryParams, string receivedHmac);
    }
}