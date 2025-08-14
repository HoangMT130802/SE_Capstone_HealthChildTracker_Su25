using AutoMapper;
using Contracts.DTOs.Authentication;
using Contracts.DTOs.Member;
using Contracts.DTOs.FacilityStaff;
using Services.Interfaces;
using Repositories.Entities;
using Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;
using Repositories.Models.QueryModels;

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

        /// <summary>
        /// Helper method để check xem role có phải là staff role không
        /// </summary>
        private static bool IsStaffRole(string role)
        {
            return role == "FacilityStaff";
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
                FacilityStaff? staffInfo = null; // Biến để lưu staff info và tái sử dụng

               
                // ✅ Lấy thông tin theo role
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
                else if (IsStaffRole(account.Role))
                {
                    
                    var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    staffInfo = await staffRepository.GetAsync(s => s.AccountId == account.AccountId, 
                        includeProperties: "Account,Facility");
                    
                    _logger.LogInformation($"Staff lookup for AccountId {account.AccountId}: {(staffInfo != null ? "Found" : "Not Found")}");
                    
                    if (staffInfo != null)
                    {
                        response.FullName = staffInfo.FullName;
                        response.Phone = staffInfo.Phone?.ToString() ?? "";
                        response.StaffId = staffInfo.StaffId;
                        response.Position = staffInfo.Position; // "Manager", "Doctor", "Staff"
                        response.FacilityId = staffInfo.FacilityId;
                        
                        _logger.LogInformation($"Staff info loaded - StaffId: {staffInfo.StaffId}, Position: {staffInfo.Position}, FacilityId: {staffInfo.FacilityId}");
                    }
                    else
                    {
                        _logger.LogWarning($"FacilityStaff record not found for AccountId {account.AccountId} with role {account.Role}");
                        // Set default values to avoid null
                        response.StaffId = null;
                        response.Position = null;
                        response.FacilityId = null;
                    }
                }

                // ✅ Generate JWT với FacilityId cho Staff (sử dụng lại staffInfo đã load)
                if (IsStaffRole(account.Role))
                {
                    if (staffInfo != null)
                    {
                        response.Token = _jwtService.GenerateToken(account, staffInfo.FacilityId);
                        _logger.LogInformation($"JWT generated with FacilityId: {staffInfo.FacilityId}");
                    }
                    else
                    {
                        response.Token = _jwtService.GenerateToken(account);
                        _logger.LogWarning($"JWT generated without FacilityId for staff account {account.AccountName}");
                    }
                }
                else
                {
                    response.Token = _jwtService.GenerateToken(account);
                }

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
                if (account.Role != "FacilityStaff") // ✅ Chỉ check "FacilityStaff"
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

                // Lấy thông tin FacilityStaff (nếu có)
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepository.GetAsync(s => s.AccountId == account.AccountId, 
                    includeProperties: "Account,Facility");
                
                StaffResponseDTO response;
                
                if (staff == null)
                {
                    // ✅ Không còn logic cho Manager mới tạo vì tất cả đều có role "FacilityStaff"
                    throw new UnauthorizedAccessException("Không tìm thấy thông tin nhân viên");
                }
                else
                {
                    if (!staff.Status)
                    {
                        throw new UnauthorizedAccessException("Tài khoản nhân viên đã bị vô hiệu hóa");
                    }

                    response = _mapper.Map<StaffResponseDTO>(staff);
                    // ✅ Sử dụng JWT với FacilityId cho Staff/Manager
                    response.Token = _jwtService.GenerateToken(account, staff.FacilityId);
                    
                    // ✅ Load DoctorProfile nếu position = "Doctor"
                    if (staff.Position.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                        var doctorProfile = await doctorProfileRepository.GetAsync(dp => dp.DoctorId == staff.StaffId);
                        if (doctorProfile != null)
                        {
                            response.DoctorProfile = _mapper.Map<DoctorProfileDTO>(doctorProfile);
                        }
                    }
                }

                _logger.LogInformation($"Staff {account.AccountName} with position {response.Position} logged in successfully");
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
                    newAccount.Role = "Member"; // Trở về logic cũ - tạo Member ngay

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync(); 

                    _logger.LogInformation($"Account created with ID: {newAccount.AccountId}");

                    // Tạo Member record ngay khi register (logic cũ)
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

                    _logger.LogInformation($"Member record created with ID: {newMember.MemberId}");

                    await transaction.CommitAsync();

                    var response = _mapper.Map<UserResponseDTO>(newAccount);
                    // Set thông tin cá nhân từ Member record
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

                await ValidateStaffCreationRequestWithPhone(request.AccountName, request.Email, request.Phone ?? "");

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Hash password
                    var hashedPassword = BC.HashPassword(request.Password);
                    
                    // ✅ Create Account với Role = "FacilityStaff"
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var newAccount = new Account
                    {
                        AccountName = request.AccountName,
                        Email = request.Email,
                        Password = hashedPassword,
                        Role = "FacilityStaff", // ✅ Manager cũng có role "FacilityStaff"
                        Status = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync();

                    // ✅ TEMPORARY FIX: Không tạo FacilityStaff record cho Manager mới
                    // Sẽ được tạo sau khi Admin assign Manager vào facility cụ thể
                    _logger.LogInformation($"Manager {newAccount.AccountName} created without FacilityStaff record. Will be created when assigned to facility.");

                    await transaction.CommitAsync();

                    // ✅ Prepare response cho Manager chưa có FacilityStaff record
                    var response = new StaffResponseDTO
                    {
                        AccountId = newAccount.AccountId,
                        StaffId = 0, // Manager chưa có FacilityStaff record
                        AccountName = newAccount.AccountName,
                        Email = newAccount.Email,
                        Role = newAccount.Role,
                        FullName = request.FullName,
                        Phone = request.Phone ?? "",
                        FacilityId = 0, // Manager chưa có facility
                        Position = "Manager",
                        Description = request.Description ?? "",
                        Status = true,
                        CreatedAt = newAccount.CreatedAt,
                        Token = _jwtService.GenerateToken(newAccount, null) // Manager mới chưa có facility
                    };

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
                // Validate manager account and get facility info
                var managerAccountRepository = _unitOfWork.GetRepository<Account>();
                var managerAccount = await managerAccountRepository.GetAsync(a => a.AccountId == managerAccountId);
                
                if (managerAccount == null)
                {
                    throw new UnauthorizedAccessException("Tài khoản Manager không tồn tại");
                }

                // ✅ Lấy thông tin facility từ Manager
                var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                var managerStaff = await staffRepository.GetAsync(s => s.AccountId == managerAccountId);
                
                if (managerStaff == null || managerStaff.Position != "Manager")
                {
                    throw new UnauthorizedAccessException("Chỉ Manager mới có quyền tạo tài khoản Staff/Doctor");
                }

                // ✅ FacilityId được lấy từ Manager, không cần từ request
                var facilityId = managerStaff.FacilityId;

                await ValidateStaffCreationRequestWithPhone(request.AccountName, request.Email, request.Phone ?? "");

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
                    // Hash password
                    var hashedPassword = BC.HashPassword(request.Password);
                    
                    // ✅ Create Account với Role = "FacilityStaff" tự động
                    var accountRepository = _unitOfWork.GetRepository<Account>();
                    var newAccount = new Account
                    {
                        AccountName = request.AccountName,
                        Email = request.Email,
                        Password = hashedPassword,
                        Role = "FacilityStaff", // ✅ Set tự động
                        Status = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await accountRepository.AddAsync(newAccount);
                    await _unitOfWork.SaveChangesAsync();

                    // ✅ Create FacilityStaff với FacilityId từ Manager
                    var newStaff = new FacilityStaff
                    {
                        AccountId = newAccount.AccountId,
                        FacilityId = facilityId, // ✅ Từ Manager
                        FullName = request.FullName,
                        Email = request.Email,
                        Phone = string.IsNullOrEmpty(request.Phone) ? (int?)null : 
                            (int.TryParse(request.Phone, out int phoneNumber) ? phoneNumber : (int?)null),
                        Position = request.Position, // "Doctor" hoặc "Staff"
                        Description = request.Description ?? "",
                        Status = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    // Log values để debug
                    _logger.LogInformation($"Creating {request.Position} for Manager's facility - FacilityId: {facilityId}, Position: {newStaff.Position}, FullName: {newStaff.FullName}");

                    await staffRepository.AddAsync(newStaff);
                    await _unitOfWork.SaveChangesAsync();

                    // ✅ Tạo DoctorProfile nếu position = "Doctor"
                    if (request.Position.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                        var doctorProfile = new DoctorProfile
                        {
                            DoctorId = newStaff.StaffId, // FK to FacilityStaff
                            Age = request.Age ?? 0,
                            Specialization = request.Specialization ?? "",
                            Certifications = request.Certifications ?? "",
                            University = request.University ?? "",
                            Bio = request.Bio ?? "",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await doctorProfileRepository.AddAsync(doctorProfile);
                        await _unitOfWork.SaveChangesAsync();
                        
                        _logger.LogInformation($"DoctorProfile created for Doctor {newStaff.FullName} (StaffId: {newStaff.StaffId})");
                    }

                    await transaction.CommitAsync();

                    // Prepare response
                    var staffWithAccount = await staffRepository.GetAsync(
                        s => s.StaffId == newStaff.StaffId, 
                        includeProperties: "Account"
                    );

                    var response = _mapper.Map<StaffResponseDTO>(staffWithAccount);
                    
                    // ✅ Load DoctorProfile nếu position = "Doctor"
                    if (newStaff.Position.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                        var doctorProfile = await doctorProfileRepository.GetAsync(dp => dp.DoctorId == newStaff.StaffId);
                        if (doctorProfile != null)
                        {
                            response.DoctorProfile = _mapper.Map<DoctorProfileDTO>(doctorProfile);
                        }
                    }
                    
                    response.Token = _jwtService.GenerateToken(newAccount, facilityId); // ✅ Include FacilityId

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
                else if (currentAccount.Role == "FacilityStaff") // ✅ Sửa check role
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
                else
                {
                    throw new UnauthorizedAccessException("Không có quyền cập nhật thông tin staff");
                }

                // Validate email uniqueness (exclude current staff)
                var staffWithSameEmail = await staffRepository.GetAsync(s => 
                    s.Email.ToLower() == request.Email.ToLower() && s.StaffId != request.StaffId);
                if (staffWithSameEmail != null)
                {
                    throw new InvalidOperationException("Email đã được sử dụng bởi staff khác");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync();
                
                try
                {
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

                    // ✅ Handle DoctorProfile nếu position = "Doctor"
                    if (request.Position.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                        var existingDoctorProfile = await doctorProfileRepository.GetAsync(dp => dp.DoctorId == staff.StaffId);
                        
                        if (existingDoctorProfile == null)
                        {
                            // Tạo DoctorProfile mới nếu chưa có
                            var newDoctorProfile = new DoctorProfile
                            {
                                DoctorId = staff.StaffId,
                                Age = request.Age ?? 0,
                                Specialization = request.Specialization ?? "",
                                Certifications = request.Certifications ?? "",
                                University = request.University ?? "",
                                Bio = request.Bio ?? "",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            await doctorProfileRepository.AddAsync(newDoctorProfile);
                            _logger.LogInformation($"Created DoctorProfile for Staff {staff.FullName} (StaffId: {staff.StaffId})");
                        }
                        else
                        {
                            // Cập nhật DoctorProfile đã tồn tại
                            existingDoctorProfile.Age = request.Age ?? existingDoctorProfile.Age;
                            existingDoctorProfile.Specialization = request.Specialization ?? existingDoctorProfile.Specialization;
                            existingDoctorProfile.Certifications = request.Certifications ?? existingDoctorProfile.Certifications;
                            existingDoctorProfile.University = request.University ?? existingDoctorProfile.University;
                            existingDoctorProfile.Bio = request.Bio ?? existingDoctorProfile.Bio;
                            existingDoctorProfile.UpdatedAt = DateTime.UtcNow;

                            doctorProfileRepository.Update(existingDoctorProfile);
                            _logger.LogInformation($"Updated DoctorProfile for Staff {staff.FullName} (StaffId: {staff.StaffId})");
                        }
                        
                        await _unitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        // ✅ Nếu position không phải Doctor, xóa DoctorProfile nếu có
                        var doctorProfileRepository = _unitOfWork.GetRepository<DoctorProfile>();
                        var existingDoctorProfile = await doctorProfileRepository.GetAsync(dp => dp.DoctorId == staff.StaffId);
                        if (existingDoctorProfile != null)
                        {
                            doctorProfileRepository.Delete(existingDoctorProfile);
                            await _unitOfWork.SaveChangesAsync();
                            _logger.LogInformation($"Deleted DoctorProfile for Staff {staff.FullName} (StaffId: {staff.StaffId}) as position changed to {request.Position}");
                        }
                    }

                    await transaction.CommitAsync();

                    var response = _mapper.Map<FacilityStaffInfoResponseDTO>(staff);
                    
                    _logger.LogInformation($"Staff {staff.FullName} info updated successfully by user {currentAccount.AccountName}");
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
        public async Task<QueryResultModel<List<MemberDTO>>> GetAllMembersAsync(int currentUserId, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                
                var accountRepository = _unitOfWork.GetRepository<Account>();
                var currentAccount = await accountRepository.GetAsync(a => a.AccountId == currentUserId);
                if (currentAccount == null || currentAccount.Role != "Admin")
                {
                    _logger.LogWarning($"Unauthorized access attempt to GetAllMembers by AccountId {currentUserId}");
                    throw new UnauthorizedAccessException("Chỉ Admin mới có quyền xem danh sách tất cả thành viên");
                }

                var memberRepository = _unitOfWork.GetRepository<Member>();
                var result = await memberRepository.GetAllAsync(
                    filter: m => m.Account.Status, // Chỉ lấy Member có Account.Status = true
                    orderBy: q => q.OrderByDescending(m => m.CreatedAt),
                    include: "Account",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var memberDTOs = _mapper.Map<List<MemberDTO>>(result.Data);

                _logger.LogInformation($"Admin {currentAccount.AccountName} retrieved {memberDTOs.Count} members (page {pageIndex}, size {pageSize})");
                return new QueryResultModel<List<MemberDTO>>
                {
                    Data = memberDTOs,
                    TotalCount = result.TotalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving all members for AccountId {currentUserId}");
                throw new Exception($"Lỗi khi lấy danh sách thành viên: {ex.Message}");
            }
        }
    }
}
