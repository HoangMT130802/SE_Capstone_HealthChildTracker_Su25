using AutoMapper;
using Contracts.DTOs.Authentication;
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
                    // TODO: Làm role facility
                    // var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
                    // var staff = await staffRepository.GetAsync(s => s.AccountId == account.AccountId);
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
    }
}
