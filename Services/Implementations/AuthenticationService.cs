using AutoMapper;
using Contracts.DTOs.Authentication;
using Contracts.DTOs.Member;
using Contracts.DTOs.FacilityStaff;
using Services.Interfaces;
using Repositories.Entities;
using Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IJwtService jwtService,
            ILogger<AuthenticationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<UserResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
                {
                    throw new ArgumentException("AccountName và mật khẩu không được để trống");
                }

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(u =>
                    (u.AccountName.ToLower() == request.AccountName.ToLower() ||
                     u.Email.ToLower() == request.AccountName.ToLower()));

                _logger.LogInformation($"Login attempt for: {request.AccountName}");
                
                if (account == null)
                {
                    _logger.LogWarning($"Account not found: {request.AccountName}");
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                _logger.LogInformation($"Account found: {account.AccountName}, checking password...");
                
              
                _logger.LogInformation($"Raw password length: {request.Password?.Length}");
                _logger.LogInformation($"Stored hash length: {account.Password?.Length}");
                _logger.LogInformation($"Raw password: '{request.Password}'");
                _logger.LogInformation($"Stored hash starts with: '{account.Password?.Substring(0, Math.Min(20, account.Password?.Length ?? 0))}'");
                
          
                string trimmedStoredPassword = account.Password?.Trim();
                _logger.LogInformation($"Trimmed hash length: {trimmedStoredPassword?.Length}");
                
                bool isPasswordValid = BC.Verify(request.Password, trimmedStoredPassword);
                _logger.LogInformation($"BCrypt verification result: {isPasswordValid}");
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Password verification failed for: {account.AccountName}");
                    _logger.LogWarning($"Expected to verify: '{request.Password}' against hash: '{account.Password}'");
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                if (!account.Status)
                {
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
                }

                var response = _mapper.Map<UserResponseDTO>(account);

               
                if (account.Role == "Member")
                {
                    var memberRepository = _unitOfWork.GetRepository<Member>();
                    var member = await memberRepository.GetAsync(m => m.AccountId == account.AccountId);
                    if (member != null)
                    {
                        response.FullName = member.FullName;
                        response.Phone = member.PhoneNumber;
                        response.Address = member.Address;
                    }
                }
                else if (account.Role == "FacilityStaff" || account.Role == "Doctor" || account.Role == "Manager")
                {
                    var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    var staff = await staffRepository.GetAsync(s => s.AccountId == account.AccountId, 
                        includeProperties: "Account,Facility");
                    if (staff != null)
                    {
                        response.FullName = staff.FullName;
                        response.Phone = staff.Phone?.ToString();
                        response.StaffId = staff.StaffId;
                        response.Position = staff.Position;
                        response.FacilityId = staff.FacilityId;
                    }
                }

                response.Token = _jwtService.GenerateToken(account);

                _logger.LogInformation($"User {account.AccountName} with role {account.Role} logged in successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login failed: {ex.Message}");
                throw;
            }
        }

        public async Task<StaffResponseDTO> LoginStaffAsync(LoginRequestDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
                {
                    throw new ArgumentException("AccountName và mật khẩu không được để trống");
                }

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(u =>
                    (u.AccountName.ToLower() == request.AccountName.ToLower() ||
                     u.Email.ToLower() == request.AccountName.ToLower()));

                _logger.LogInformation($"Staff login attempt for: {request.AccountName}");
                
                if (account == null)
                {
                    _logger.LogWarning($"Account not found: {request.AccountName}");
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                // Kiểm tra role có phải FacilityStaff không
                if (account.Role != "FacilityStaff" && account.Role != "Doctor" && account.Role != "Manager")
                {
                    throw new UnauthorizedAccessException("Tài khoản này không phải là nhân viên cơ sở");
                }

                _logger.LogInformation($"Staff account found: {account.AccountName}, checking password...");
                
                string trimmedStoredPassword = account.Password?.Trim();
                bool isPasswordValid = BC.Verify(request.Password, trimmedStoredPassword);
                _logger.LogInformation($"BCrypt verification result: {isPasswordValid}");
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Password verification failed for: {account.AccountName}");
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                if (!account.Status)
                {
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
                }

                // Lấy thông tin FacilityStaff
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepository.GetAsync(s => s.AccountId == account.AccountId, 
                    includeProperties: "Account,Facility");
                
                if (staff == null)
                {
                    throw new UnauthorizedAccessException("Không tìm thấy thông tin nhân viên");
                }

                if (!staff.Status)
                {
                    throw new UnauthorizedAccessException("Tài khoản nhân viên đã bị vô hiệu hóa");
                }

                var response = _mapper.Map<StaffResponseDTO>(staff);
                response.Token = _jwtService.GenerateToken(account);

                _logger.LogInformation($"Staff {account.AccountName} with position {staff.Position} logged in successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Staff login failed: {ex.Message}");
                throw;
            }
        }

        public async Task<UserResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            try
            {
                await ValidateRegistrationRequest(request);

             
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Hash mật khẩu
                    _logger.LogInformation($"Register - Raw password: '{request.Password}' (length: {request.Password?.Length})");
                    var hashedPassword = BC.HashPassword(request.Password);
                    _logger.LogInformation($"Register - Hashed password: '{hashedPassword}' (length: {hashedPassword?.Length})");
                    
                   
                    bool immediateVerification = BC.Verify(request.Password, hashedPassword);
                    _logger.LogInformation($"Register - Immediate verification test: {immediateVerification}");
                    
                    // Tạo Account
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var newAccount = _mapper.Map<Account>(request);
                    newAccount.Password = hashedPassword;
                    newAccount.CreatedAt = DateTime.UtcNow;
                    newAccount.UpdatedAt = DateTime.UtcNow;
                    newAccount.Status = true;
                    newAccount.Role = "Member"; 

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync(); 

                  
                    _logger.LogInformation($"Account created with ID: {newAccount.AccountId}");

                    if (newAccount.Role == "Member")
                    {
                        var memberRepository = _unitOfWork.GetRepository<Member>();
                        var newMember = new Member
                        {
                            AccountId = newAccount.AccountId, 
                            FullName = request.FullName,
                            PhoneNumber = request.Phone,
                            Address = request.Address,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await memberRepository.AddAsync(newMember);
                        await _unitOfWork.SaveChangesAsync();
                        
                        _logger.LogInformation($"Member created for AccountId: {newAccount.AccountId}");
                    }

                    await transaction.CommitAsync();

                    var response = _mapper.Map<UserResponseDTO>(newAccount);
                    response.FullName = request.FullName;
                    response.Phone = request.Phone;
                    response.Address = request.Address;
                    response.Token = _jwtService.GenerateToken(newAccount);

                    _logger.LogInformation($"User {newAccount.AccountName} registered successfully with role {newAccount.Role}");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration failed: {ex.Message}");
                throw;
            }
        }

        private async Task ValidateRegistrationRequest(RegisterRequestDTO request)
        {
            var accountRepository = _unitOfWork.GetRepository<Account>();

            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                throw new ArgumentException("AccountName và password không được để trống");
            }

            if (!IsValidEmail(request.Email))
            {
                throw new ArgumentException("Email không hợp lệ");
            }

            var existingUsername = await accountRepository.GetAsync(u => 
                u.AccountName.ToLower() == request.AccountName.ToLower());
            if (existingUsername != null)
            {
                throw new InvalidOperationException("AccountName đã tồn tại");
            }

            var existingEmail = await accountRepository.GetAsync(u => 
                u.Email.ToLower() == request.Email.ToLower());
            if (existingEmail != null)
            {
                throw new InvalidOperationException("Email đã tồn tại");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public async Task<StaffResponseDTO> CreateManagerAsync(CreateManagerDTO request, int adminAccountId)
        {
            try
            {
                // Validate admin account
                var adminAccountRepository = _unitOfWork.GetRepository<Account>();
                var adminAccount = await adminAccountRepository.GetAsync(a => a.AccountId == adminAccountId);
                
                if (adminAccount == null || adminAccount.Role != "Admin")
                {
                    throw new UnauthorizedAccessException("Chỉ Admin mới có quyền tạo tài khoản Manager");
                }

                await ValidateStaffCreationRequestWithPhone(request.AccountName, request.Email, request.Phone);

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Hash password
                    var hashedPassword = BC.HashPassword(request.Password);
                    
                    // Create Account
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var newAccount = _mapper.Map<Account>(request);
                    newAccount.Password = hashedPassword;
                    newAccount.CreatedAt = DateTime.UtcNow;
                    newAccount.UpdatedAt = DateTime.UtcNow;

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync();

                    // Create FacilityStaff
                    var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    var newStaff = _mapper.Map<FacilityStaff>(request);
                    newStaff.AccountId = newAccount.AccountId;
                    newStaff.Email = request.Email;
                    newStaff.Phone = string.IsNullOrEmpty(request.Phone) ? (int?)null : 
                        (int.TryParse(request.Phone, out int phoneNumber) ? phoneNumber : (int?)null);
                    newStaff.CreatedAt = DateTime.UtcNow;
                    newStaff.UpdatedAt = DateTime.UtcNow;

                    await staffRepository.AddAsync(newStaff);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Prepare response
                    var staffWithAccount = await staffRepository.GetAsync(
                        s => s.StaffId == newStaff.StaffId, 
                        includeProperties: "Account"
                    );

                    var response = _mapper.Map<StaffResponseDTO>(staffWithAccount);
                    response.Token = _jwtService.GenerateToken(newAccount);

                    _logger.LogInformation($"Manager {newAccount.AccountName} created successfully by Admin {adminAccount.AccountName}");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create manager failed: {ex.Message}");
                throw;
            }
        }

        public async Task<StaffResponseDTO> CreateStaffAsync(CreateStaffDTO request, int managerAccountId)
        {
            try
            {
                // Validate manager account and facility access
                var managerAccountRepository = _unitOfWork.GetRepository<Account>();
                var managerAccount = await managerAccountRepository.GetAsync(a => a.AccountId == managerAccountId);
                
                if (managerAccount == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản Manager không tồn tại");
                }

                // Check if manager belongs to the same facility and has Manager position
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var managerStaff = await staffRepository.GetAsync(s => s.AccountId == managerAccountId);
                
                if (managerStaff == null || managerStaff.Position != "Manager")
                {
                    throw new UnauthorizedAccessException("Chỉ Manager mới có quyền tạo tài khoản Staff/Doctor");
                }
                
                if (managerStaff.FacilityId != request.FacilityId)
                {
                    throw new UnauthorizedAccessException("Manager chỉ có thể tạo tài khoản cho cơ sở y tế mà mình quản lý");
                }

                await ValidateStaffCreationRequestWithPhone(request.AccountName, request.Email, request.Phone);

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Hash password
                    var hashedPassword = BC.HashPassword(request.Password);
                    
                    // Create Account
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var newAccount = _mapper.Map<Account>(request);
                    newAccount.Password = hashedPassword;
                    newAccount.CreatedAt = DateTime.UtcNow;
                    newAccount.UpdatedAt = DateTime.UtcNow;

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync();

                    // Create FacilityStaff
                    var newStaff = _mapper.Map<FacilityStaff>(request);
                    newStaff.AccountId = newAccount.AccountId;
                    newStaff.Email = request.Email;
                    newStaff.Phone = string.IsNullOrEmpty(request.Phone) ? (int?)null : 
                        (int.TryParse(request.Phone, out int phoneNumber) ? phoneNumber : (int?)null);
                    newStaff.CreatedAt = DateTime.UtcNow;
                    newStaff.UpdatedAt = DateTime.UtcNow;

                    await staffRepository.AddAsync(newStaff);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Prepare response
                    var staffWithAccount = await staffRepository.GetAsync(
                        s => s.StaffId == newStaff.StaffId, 
                        includeProperties: "Account"
                    );

                    var response = _mapper.Map<StaffResponseDTO>(staffWithAccount);
                    response.Token = _jwtService.GenerateToken(newAccount);

                    _logger.LogInformation($"{request.Position} {newAccount.AccountName} created successfully by Manager {managerAccount.AccountName}");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create staff failed: {ex.Message}");
                throw;
            }
        }

        private async Task ValidateStaffCreationRequest(string accountName, string email)
        {
            var accountRepository = _unitOfWork.GetRepository<Account>();

            if (string.IsNullOrEmpty(accountName))
            {
                throw new ArgumentException("Tên tài khoản không được để trống");
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Email không hợp lệ");
            }

            // Check if account name already exists
            var existingAccountByName = await accountRepository.GetAsync(u => u.AccountName.ToLower() == accountName.ToLower());
            if (existingAccountByName != null)
            {
                throw new InvalidOperationException("Tên tài khoản đã tồn tại");
            }

            // Check if email already exists
            var existingAccountByEmail = await accountRepository.GetAsync(u => u.Email.ToLower() == email.ToLower());
            if (existingAccountByEmail != null)
            {
                throw new InvalidOperationException("Email đã được sử dụng");
            }
        }

        private async Task ValidateStaffCreationRequestWithPhone(string accountName, string email, string phone)
        {
            await ValidateStaffCreationRequest(accountName, email);

            // Validate phone format if provided
            if (!string.IsNullOrEmpty(phone))
            {
                if (!int.TryParse(phone, out _))
                {
                    throw new ArgumentException("Số điện thoại không hợp lệ - chỉ được chứa số");
                }
            }
        }

        public async Task<MemberInfoResponseDTO> UpdateMemberInfoAsync(UpdateMemberInfoDTO request, int currentUserId)
        {
            try
            {
                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == currentUserId, includeProperties: "Account");
                
                if (member == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản Member không tồn tại");
                }

                // Validate email uniqueness (exclude current user)
                var accountRepository = _unitOfWork.GetRepository<Account>();
                var existingAccountByEmail = await accountRepository.GetAsync(u => 
                    u.Email.ToLower() == request.Email.ToLower() && u.AccountId != currentUserId);
                if (existingAccountByEmail != null)
                {
                    throw new InvalidOperationException("Email đã được sử dụng");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Update account email
                    member.Account.Email = request.Email;
                    member.Account.UpdatedAt = DateTime.UtcNow;
                    accountRepository.Update(member.Account);

                    // Update member info
                    member.FullName = request.FullName;
                    member.PhoneNumber = request.PhoneNumber;
                    member.Address = request.Address;
                    member.UpdatedAt = DateTime.UtcNow;
                    memberRepository.Update(member);

                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var response = _mapper.Map<MemberInfoResponseDTO>(member);
                    
                    _logger.LogInformation($"Member {member.FullName} updated their info successfully");
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update member info failed: {ex.Message}");
                throw;
            }
        }

        public async Task<FacilityStaffInfoResponseDTO> UpdateFacilityStaffInfoAsync(UpdateFacilityStaffInfoDTO request, int currentUserId)
        {
            try
            {
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepository.GetAsync(s => s.StaffId == request.StaffId, includeProperties: "Account");
                
                if (staff == null)
                {
                    throw new ArgumentException("Staff không tồn tại");
                }

                // Check permission: Admin can update all, Manager can update in their facility, user can update themselves
                var currentAccountRepository = _unitOfWork.GetRepository<Account>();
                var currentAccount = await currentAccountRepository.GetAsync(a => a.AccountId == currentUserId);
                
                if (currentAccount == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản không hợp lệ");
                }

                if (currentAccount.Role == "Admin")
                {
                    // Admin can update all
                }
                else
                {
                    // Check if current user is Manager
                    var currentStaff = await staffRepository.GetAsync(s => s.AccountId == currentUserId);
                    if (currentStaff != null && currentStaff.Position == "Manager")
                    {
                        // Manager can only update staff in their facility
                        if (currentStaff.FacilityId != staff.FacilityId)
                        {
                            throw new UnauthorizedAccessException("Manager chỉ có thể cập nhật thông tin staff trong cơ sở y tế của mình");
                        }
                    }
                    else if (staff.AccountId != currentUserId)
                    {
                        // Other users can only update themselves
                        throw new UnauthorizedAccessException("Bạn chỉ có thể cập nhật thông tin của chính mình");
                    }
                }

                // Validate email uniqueness (exclude current staff)
                var staffWithSameEmail = await staffRepository.GetAsync(s => 
                    s.Email.ToLower() == request.Email.ToLower() && s.StaffId != request.StaffId);
                if (staffWithSameEmail != null)
                {
                    throw new InvalidOperationException("Email đã được sử dụng bởi staff khác");
                }

                // Update staff info
                staff.FullName = request.FullName;
                staff.Phone = string.IsNullOrEmpty(request.Phone) ? (int?)null : 
                    (int.TryParse(request.Phone, out int phoneNumber) ? phoneNumber : (int?)null);
                staff.Email = request.Email;
                staff.Position = request.Position;
                staff.Description = request.Description;
                staff.Status = request.Status;
                staff.UpdatedAt = DateTime.UtcNow;

                staffRepository.Update(staff);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<FacilityStaffInfoResponseDTO>(staff);
                
                _logger.LogInformation($"Staff {staff.FullName} info updated successfully by user {currentAccount.AccountName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update staff info failed: {ex.Message}");
                throw;
            }
        }

        public async Task<UserResponseDTO> BanUserAsync(BanUserRequestDTO request, int currentUserId)
        {
            try
            {
                var currentAccountRepository = _unitOfWork.GetRepository<Account>();
                var currentAccount = await currentAccountRepository.GetAsync(a => a.AccountId == currentUserId);
                
                if (currentAccount == null || currentAccount.Role != "Admin")
                {
                    throw new UnauthorizedAccessException("Chỉ Admin mới có quyền ban/unban tài khoản");
                }

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var targetAccount = await accountRepository.GetAsync(a => a.AccountId == request.AccountId);
                
                if (targetAccount == null)
                {
                    throw new ArgumentException("Tài khoản không tồn tại");
                }

                if (targetAccount.AccountId == currentUserId)
                {
                    throw new InvalidOperationException("Không thể ban chính mình");
                }

                // Update account status
                targetAccount.Status = request.Status;
                targetAccount.UpdatedAt = DateTime.UtcNow;

                accountRepository.Update(targetAccount);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<UserResponseDTO>(targetAccount);
                
                string action = request.Status ? "unban" : "ban";
                _logger.LogInformation($"Admin {currentAccount.AccountName} {action} user {targetAccount.AccountName}. Reason: {request.Reason}");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ban user failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteStaffAsync(int staffId, int managerAccountId)
        {
            try
            {
                var managerAccountRepository = _unitOfWork.GetRepository<Account>();
                var managerAccount = await managerAccountRepository.GetAsync(a => a.AccountId == managerAccountId);
                
                if (managerAccount == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản Manager không tồn tại");
                }

                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                
                // Check if manager has Manager position
                var managerStaff = await staffRepository.GetAsync(s => s.AccountId == managerAccountId);
                if (managerStaff == null || managerStaff.Position != "Manager")
                {
                    throw new UnauthorizedAccessException("Chỉ Manager mới có quyền xóa staff/doctor");
                }

                var staff = await staffRepository.GetAsync(s => s.StaffId == staffId, includeProperties: "Account");
                
                if (staff == null)
                {
                    throw new ArgumentException("Staff không tồn tại");
                }
                if (managerStaff.FacilityId != staff.FacilityId)
                {
                    throw new UnauthorizedAccessException("Manager chỉ có thể xóa staff/doctor trong cơ sở y tế của mình");
                }

                // Cannot delete manager
                if (staff.Position == "Manager")
                {
                    throw new InvalidOperationException("Không thể xóa tài khoản Manager");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Delete FacilityStaff first
                    staffRepository.Delete(staff);
                    await _unitOfWork.SaveChangesAsync();

                    // Then delete Account
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    accountRepository.Delete(staff.Account);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Manager {managerAccount.AccountName} deleted {staff.Position} {staff.FullName} (StaffId: {staffId})");
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete staff failed: {ex.Message}");
                throw;
            }
        }
    }
}
