using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVaccinationFacilityPaymentAccountService
    {
        Task<int> CreatePaymentAccountAsync(CreateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId);
        Task UpdatePaymentAccountAsync(int id, UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId);
        Task DeletePaymentAccountAsync(int id, int accountId);
        Task<VaccinationFacilityPaymentAccountDto> GetPaymentAccountByIdAsync(int id);
        Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetAllPaymentAccountsAsync(int? pageIndex = null, int? pageSize = null);
        Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetPaymentAccountByFacilityIdAsync(int facilityId, int? pageIndex = null, int? pageSize = null);
    }
}
