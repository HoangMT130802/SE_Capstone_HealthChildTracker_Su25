using AutoMapper;
using Contracts.DTOs.Authentication;
using Contracts.DTOs.Member;
using Contracts.DTOs.FacilityStaff;
using Repositories.Entities;

namespace Contracts.MapperProfiles
{
    public class AuthenticationProfile : Profile
    {
        public AuthenticationProfile()
        {
            CreateMap<Account, UserResponseDTO>()
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.AccountName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.Ignore()) 
                .ForMember(dest => dest.Phone, opt => opt.Ignore()) 
                .ForMember(dest => dest.Address, opt => opt.Ignore()) 
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Token, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.StaffId, opt => opt.Ignore()) // Sẽ được set manual trong service
                .ForMember(dest => dest.Position, opt => opt.Ignore()) // Sẽ được set manual trong service
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore()); // Sẽ được set manual trong service

            CreateMap<RegisterRequestDTO, Account>()
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityStaffs, opt => opt.Ignore())
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.UserMemberships, opt => opt.Ignore());

            // Mapper cho CreateManagerDTO
            CreateMap<CreateManagerDTO, Account>()
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "Manager"))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityStaffs, opt => opt.Ignore())
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.UserMemberships, opt => opt.Ignore());

            CreateMap<CreateManagerDTO, FacilityStaff>()
                .ForMember(dest => dest.StaffId, opt => opt.Ignore())
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.FacilityId, opt => opt.Ignore()) // Manager chưa có facility khi tạo
                .ForMember(dest => dest.Phone, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => "Manager"))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Account, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorProfile, opt => opt.Ignore())
                .ForMember(dest => dest.Facility, opt => opt.Ignore());

            // ✅ Bỏ mapping CreateStaffDTO vì giờ tạo manual trong service
            // CreateStaffDTO -> Account và FacilityStaff được handle trong AuthenticationService

            // Mapper cho response
            CreateMap<FacilityStaff, StaffResponseDTO>()
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.StaffId, opt => opt.MapFrom(src => src.StaffId))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Account.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Account.Role))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone.HasValue ? src.Phone.Value.ToString() : ""))
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Token, opt => opt.Ignore());

            // Mapper cho MemberInfoResponseDTO
            CreateMap<Member, MemberInfoResponseDTO>()
                .ForMember(dest => dest.MemberId, opt => opt.MapFrom(src => src.MemberId))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Account.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Account.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Account.Status))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // Mapper cho FacilityStaffInfoResponseDTO
            CreateMap<FacilityStaff, FacilityStaffInfoResponseDTO>()
                .ForMember(dest => dest.StaffId, opt => opt.MapFrom(src => src.StaffId))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.AccountName))
                .ForMember(dest => dest.FacilityId, opt => opt.MapFrom(src => src.FacilityId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone.HasValue ? src.Phone.Value.ToString() : ""))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Account.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // ✅ Mapper cho DoctorProfile
            CreateMap<DoctorProfile, DoctorProfileDTO>()
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.Certifications, opt => opt.MapFrom(src => src.Certifications))
                .ForMember(dest => dest.University, opt => opt.MapFrom(src => src.University))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
        }
    }
} 