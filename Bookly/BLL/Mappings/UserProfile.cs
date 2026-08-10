using AutoMapper;
using BLL.DTOs.Account;
using BLL.DTOs.Auth;
using BLL.ViewModels.Account;
using BLL.ViewModels.Admin;
using BLL.ViewModels.Common;
using DAL.Enums;
using DAL.Models.Identity;


namespace BLL.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<ApplicationUser, UserSummaryViewModel>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(d => d.MemberSince, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.IsVerifiedHost, o => o.MapFrom(s =>
                s.HostVerification != null && s.HostVerification.Status == HostVerificationStatus.Verified));

        CreateMap<ApplicationUser, ProfileViewModel>();
        CreateMap<ProfileViewModel, ApplicationUser>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore());

        CreateMap<ApplicationUser, UserAdminRowViewModel>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(d => d.IsLockedOut, o => o.MapFrom(s => s.LockoutEnd != null && s.LockoutEnd > DateTimeOffset.UtcNow));

        CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email))
                .ForMember(d => d.IsHost, o => o.MapFrom(_ => false))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow));

        CreateMap<RegisterViewModel, RegisterDto>();

        CreateMap<RegisterViewModel, ApplicationUser>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email))
            .ForMember(d => d.IsHost, o => o.MapFrom(_ => false))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(_ => DateTime.UtcNow));

        CreateMap<ApplicationUser, ProfileDto>();

        CreateMap<ProfileDto, ProfileViewModel>().ReverseMap();
    }
}
