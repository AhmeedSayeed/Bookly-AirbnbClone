using AutoMapper;
using BLL.ViewModels.Payments;
using DAL.Enums;
using DAL.Models.Reservations;

namespace BLL.Mapping;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, PaymentResultViewModel>()
            .ForMember(d => d.Success, o => o.MapFrom(s => s.Status == PaymentStatus.Success))
            .ForMember(d => d.TransactionId, o => o.MapFrom(s => s.PaymobTransactionId))
            .ForMember(d => d.FailureReason, o => o.Ignore());
    }
}
