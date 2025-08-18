using AutoMapper;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Contracts.DTOs.Transaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Transaction = Repositories.Entities.Transaction;

public class VaccinationFacilityPaymentAccountService : IVaccinationFacilityPaymentAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VaccinationFacilityPaymentAccountService> _logger;
    private readonly IConfiguration _configuration;

    public VaccinationFacilityPaymentAccountService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger<VaccinationFacilityPaymentAccountService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    private async Task ValidateManagerAccess(int accountId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Manager");
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User with AccountId {accountId} is not a Manager or does not belong to Facility");
        }
    }

    /// <summary>
    /// Lấy PayOS instance cho facility cụ thể
    /// </summary>
    private async Task<PayOS> GetFacilityPayOSInstanceAsync(int facilityId)
    {
        var paymentAccount = await GetActiveFacilityPaymentAccountAsync(facilityId);
        return new PayOS(paymentAccount.ClientId, paymentAccount.ApiKey, paymentAccount.ChecksumKey);
    }

    /// <summary>
    /// Lấy thông tin PayOS account đang active của facility
    /// </summary>
    private async Task<VaccinationFacilityPaymentAccount> GetActiveFacilityPaymentAccountAsync(int facilityId)
    {
        var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
        var paymentAccount = await repository.GetAsync(pa => pa.FacilityId == facilityId && pa.IsActive == "true");
        
        if (paymentAccount == null)
        {
            throw new InvalidOperationException($"Facility {facilityId} chưa cấu hình PayOS account hoặc account không active");
        }

        if (string.IsNullOrEmpty(paymentAccount.ClientId) || 
            string.IsNullOrEmpty(paymentAccount.ApiKey) || 
            string.IsNullOrEmpty(paymentAccount.ChecksumKey))
        {
            throw new InvalidOperationException($"PayOS configuration không đầy đủ cho facility {facilityId}");
        }

        return paymentAccount;
    }

    public async Task<int> CreatePaymentAccountAsync(CreateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            
            // Nếu tạo account active, deactivate các account khác của facility này
            if (paymentAccountDto.IsActive)
            {
                var existingAccountsResult = await repository.GetAllAsync(
                    filter: pa => pa.FacilityId == paymentAccountDto.FacilityId,
                    orderBy: null,
                    pageIndex: null,
                    pageSize: null
                );

                var activeAccountIds = existingAccountsResult.Data
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList();

                if (activeAccountIds.Any())
                {
                    foreach (var id in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == id);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            var paymentAccount = _mapper.Map<VaccinationFacilityPaymentAccount>(paymentAccountDto);
            await repository.AddAsync(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created payment account with ID {paymentAccount.Id} by AccountId {accountId}");
            return paymentAccount.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating payment account by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task UpdatePaymentAccountAsync(int id, UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            if (paymentAccountDto.IsActive && paymentAccount.IsActive != "true")
            {
                var existingAccountsResult = await repository.GetAllAsync(
                    filter: pa => pa.FacilityId == paymentAccount.FacilityId && pa.Id != id,
                    orderBy: null,
                    pageIndex: null,
                    pageSize: null
                );
                var activeAccountIds = existingAccountsResult.Data
                    .Where(pa => pa.IsActive == "true")
                    .Select(pa => pa.Id)
                    .ToList();

                if (activeAccountIds.Any())
                {
                    foreach (var accountIdToUpdate in activeAccountIds)
                    {
                        var accountToUpdate = await repository.GetAsync(pa => pa.Id == accountIdToUpdate);
                        if (accountToUpdate != null)
                        {
                            accountToUpdate.IsActive = "false";
                            repository.Update(accountToUpdate);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // Update fields từ DTO
            _mapper.Map(paymentAccountDto, paymentAccount);

            repository.Update(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Updated payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeletePaymentAccountAsync(int id, int accountId)
    {
        try
        {
            await ValidateManagerAccess(accountId);

            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            // Xóa PayOS account - không cần xóa file

            repository.Delete(paymentAccount);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Deleted payment account with ID {id} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting payment account with ID {id} by AccountId {accountId}: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccinationFacilityPaymentAccountDto> GetPaymentAccountByIdAsync(int id)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccount = await repository.GetAsync(pa => pa.Id == id);
            if (paymentAccount == null)
                throw new KeyNotFoundException($"Payment account with ID {id} not found");

            return _mapper.Map<VaccinationFacilityPaymentAccountDto>(paymentAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment account with ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetAllPaymentAccountsAsync(bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: isActive.HasValue ? pa => pa.IsActive == (isActive.Value ? "true" : "false") : null,
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting all payment accounts: {ex.Message}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>> GetPaymentAccountByFacilityIdAsync(int facilityId, bool? isActive = null, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<VaccinationFacilityPaymentAccount>();
            var paymentAccountsResult = await repository.GetAllAsync(
                filter: pa => pa.FacilityId == facilityId && (!isActive.HasValue || pa.IsActive == (isActive.Value ? "true" : "false")),
                orderBy: null,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var dtos = _mapper.Map<IEnumerable<VaccinationFacilityPaymentAccountDto>>(paymentAccountsResult.Data);
            return new QueryResultModel<IEnumerable<VaccinationFacilityPaymentAccountDto>>
            {
                TotalCount = paymentAccountsResult.TotalCount,
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment accounts for FacilityId {facilityId}: {ex.Message}");
            throw;
        }
    }

    #region Payment Methods

    /// <summary>
    /// Tạo payment link thống nhất cho Order/Package/Individual Vaccine
    /// </summary>
    public async Task<FacilityPaymentResponseDTO> CreateFacilityPaymentAsync(CreateFacilityPaymentDTO request, int accountId)
    {
        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Bắt đầu tạo facility payment - Type: {PaymentType}, FacilityId: {FacilityId}, AppointmentId: {AppointmentId}", 
                request.PaymentType, request.FacilityId, request.AppointmentId);

            // Validate quyền truy cập facility
            await ValidateFacilityAccess(accountId, request.FacilityId);

            // Validate appointment thuộc facility
            await ValidateAppointmentFacility(request.AppointmentId, request.FacilityId);

            // Tính toán amount và tạo/validate Order
            var (amount, description, orderId) = await CalculatePaymentAmount(request);

            // Tạo PayOS payment link
            var payOS = await GetFacilityPayOSInstanceAsync(request.FacilityId);
            var orderCode = GenerateOrderCode(request.FacilityId, request.AppointmentId, orderId);

            var paymentData = new PaymentData(
                long.Parse(orderCode.Split('_')[0]), // timestamp part
                (int)amount,
                TruncateDescription(description),
                new List<ItemData>(),
                $"{GetBaseUrl()}/payment/cancel?orderId={orderCode}",
                $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                null
            );

            var createPayment = await payOS.createPaymentLink(paymentData);

            // Tạo Transaction record
            var transaction = new Transaction
            {
                TransactionType = request.PaymentType,
                Amount = amount,
                PaymentMethod = "PAYOS",
                TransactionCode = orderCode,
                Description = description,
                Status = "PENDING",
                CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            await transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("✅ Tạo facility payment thành công. OrderCode: {OrderCode}, PaymentUrl: {PaymentUrl}", 
                orderCode, createPayment.checkoutUrl);

            return new FacilityPaymentResponseDTO
            {
                PaymentUrl = createPayment.checkoutUrl,
                OrderCode = orderCode,
                Amount = amount,
                Status = "PENDING",
                ReturnUrl = $"{GetBaseUrl()}/payment/success?orderId={orderCode}",
                CancelUrl = $"{GetBaseUrl()}/payment/cancel?orderId={orderCode}",
                OrderId = orderId,
                AppointmentId = request.AppointmentId,
                PaymentType = request.PaymentType,
                TransactionId = transaction.TransactionId,
                Description = description
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi tạo facility payment");
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra trạng thái thanh toán và cập nhật
    /// </summary>
    public async Task<PaymentStatusDTO> CheckFacilityPaymentStatusAsync(string orderCode, int facilityId)
    {
        try
        {
            _logger.LogInformation("Kiểm tra trạng thái facility payment - OrderCode: {OrderCode}, FacilityId: {FacilityId}", 
                orderCode, facilityId);

            var transactionRepo = _unitOfWork.GetRepository<Transaction>();
            var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderCode);
            
            if (transaction == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy transaction với OrderCode: {orderCode}");
            }

            // Chỉ check PayOS nếu chưa PAID
            if (!string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                var payOS = await GetFacilityPayOSInstanceAsync(facilityId);
                var payInfo = await payOS.getPaymentLinkInformation(long.Parse(orderCode.Split('_')[0]));
                
                _logger.LogInformation("PayOS status: {Status} for OrderCode: {OrderCode}", payInfo.status, orderCode);

                if (payInfo.status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Status = "PAID";
                    transaction.Amount = payInfo.amount;
                    transactionRepo.Update(transaction);

                    // Cập nhật appointment status thành Paid
                    await UpdateAppointmentToPaid(orderCode);
                    
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ Transaction {OrderCode} updated to PAID", orderCode);
                }
                else if (payInfo.status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Status = "CANCELLED";
                    transactionRepo.Update(transaction);
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("❌ Transaction {OrderCode} updated to CANCELLED", orderCode);
                }
            }

            return new PaymentStatusDTO
            {
                Success = string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase),
                Status = transaction.Status,
                Message = GetPaymentStatusMessage(transaction.Status),
                Amount = transaction.Amount,
                PaidAt = string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái facility payment - OrderCode: {OrderCode}", orderCode);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task ValidateFacilityAccess(int accountId, int facilityId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.FacilityId == facilityId);
        
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User {accountId} không có quyền truy cập facility {facilityId}");
        }
    }

    private async Task ValidateAppointmentFacility(int appointmentId, int facilityId)
    {
        var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
        var appointment = await appointmentRepo.GetAsync(
            a => a.AppointmentId == appointmentId, 
            includeProperties: "Schedule");

        if (appointment?.Schedule?.FacilityId != facilityId)
        {
            throw new ArgumentException($"Appointment {appointmentId} không thuộc về facility {facilityId}");
        }
    }

    private async Task<(decimal amount, string description, int? orderId)> CalculatePaymentAmount(CreateFacilityPaymentDTO request)
    {
        return request.PaymentType switch
        {
            "ORDER" => await CalculateOrderPayment(request.OrderId!.Value),
            "PACKAGE" => await CalculatePackagePayment(request.PackageId!.Value, request.ChildIds!),
            "INDIVIDUAL_VACCINE" => await CalculateIndividualVaccinePayment(request.FacilityVaccineIds!, request.ChildIds!),
            _ => throw new ArgumentException($"PaymentType không hợp lệ: {request.PaymentType}")
        };
    }

    private async Task<(decimal amount, string description, int? orderId)> CalculateOrderPayment(int orderId)
    {
        var orderRepo = _unitOfWork.GetRepository<Order>();
        var order = await orderRepo.GetByIdAsync(orderId);
        
        if (order == null)
        {
            throw new ArgumentException($"Không tìm thấy Order {orderId}");
        }

        if (order.Status == "Paid")
        {
            throw new InvalidOperationException($"Order {orderId} đã được thanh toán");
        }

        return (order.TotalAmount, $"Thanh toan don hang #{orderId}", orderId);
    }

    private async Task<(decimal amount, string description, int? orderId)> CalculatePackagePayment(int packageId, List<int> childIds)
    {
        var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
        var package = await packageRepo.GetByIdAsync(packageId);
        
        if (package == null)
        {
            throw new ArgumentException($"Không tìm thấy VaccinePackage {packageId}");
        }

        var amount = package.Price * childIds.Count;
        var description = $"Goi vaccine {package.Name}";

        // TODO: Tạo Order mới cho package payment
        // var newOrderId = await CreateOrderForPackage(packageId, childIds);

        return (amount, description, null);
    }

    private async Task<(decimal amount, string description, int? orderId)> CalculateIndividualVaccinePayment(List<int> facilityVaccineIds, List<int> childIds)
    {
        var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
        decimal totalAmount = 0;
        var vaccineNames = new List<string>();

        foreach (var vaccineId in facilityVaccineIds)
        {
            var facilityVaccine = await facilityVaccineRepo.GetAsync(
                fv => fv.FacilityVaccineId == vaccineId,
                includeProperties: "Vaccine");

            if (facilityVaccine != null)
            {
                totalAmount += facilityVaccine.Price * childIds.Count;
                vaccineNames.Add(facilityVaccine.Vaccine?.Name ?? "Unknown");
            }
        }

        var description = $"Vaccine le: {string.Join(", ", vaccineNames)}";

        // TODO: Tạo Order mới cho individual vaccine payment
        // var newOrderId = await CreateOrderForIndividualVaccines(facilityVaccineIds, childIds);

        return (totalAmount, description, null);
    }

    private async Task UpdateAppointmentToPaid(string orderCode)
    {
        // Parse appointmentId từ orderCode
        var parts = orderCode.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var appointmentId))
        {
            var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
            var appointment = await appointmentRepo.GetByIdAsync(appointmentId);
            
            if (appointment != null && appointment.Status == "Approval")
            {
                appointment.Status = "Paid";
                appointment.UpdatedAt = DateTime.UtcNow;
                appointmentRepo.Update(appointment);
                
                _logger.LogInformation("✅ Updated Appointment {AppointmentId} status to Paid", appointmentId);
            }
        }
    }

    private string GenerateOrderCode(int facilityId, int appointmentId, int? orderId)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        return orderId.HasValue 
            ? $"{timestamp}_{facilityId}_{appointmentId}_{orderId}"
            : $"{timestamp}_{facilityId}_{appointmentId}";
    }

    private string TruncateDescription(string description)
    {
        return description.Length > 25 ? description.Substring(0, 25) : description;
    }

    private string GetPaymentStatusMessage(string status)
    {
        return status switch
        {
            "PAID" => "Thanh toán thành công",
            "CANCELLED" => "Thanh toán đã bị hủy",
            "PENDING" => "Đang chờ thanh toán",
            "FAILED" => "Thanh toán thất bại",
            _ => "Trạng thái không xác định"
        };
    }

    private string GetBaseUrl()
    {
        return _configuration["BaseUrl"] ?? "https://localhost:7000";
    }

    #endregion
}