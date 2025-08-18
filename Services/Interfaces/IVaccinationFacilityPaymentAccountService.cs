using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Contracts.DTOs.Transaction;
using Repositories.Models.QueryModels;

namespace Services.Interfaces
{
    public interface IVaccinationFacilityPaymentAccountService
    {
        // CRUD methods for PayOS account management
        Task<int> CreatePaymentAccountAsync(CreateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId);
        Task UpdatePaymentAccountAsync(int id, UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId);
        Task DeletePaymentAccountAsync(int id, int accountId);
        Task<VaccinationFacilityPaymentAccountDto> GetPaymentAccountByIdAsync(int id);
        Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetAllPaymentAccountsAsync(bool? isActive = null, int? pageIndex = null, int? pageSize = null);
        Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetPaymentAccountByFacilityIdAsync(int facilityId, bool? isActive = null, int? pageIndex = null, int? pageSize = null);

        // Payment methods
        Task<FacilityPaymentResponseDTO> CreateFacilityPaymentAsync(CreateFacilityPaymentDTO request, int accountId);
        Task<PaymentStatusDTO> CheckFacilityPaymentStatusAsync(string orderCode, int facilityId);
    }
}
