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

                if (account == null || !BC.Verify(request.Password, account.Password))
                {
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                if (!account.Status)
                {
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
                }

                var response = _mapper.Map<UserResponseDTO>(account);
                response.Token = _jwtService.GenerateToken(account);

                _logger.LogInformation($"User {account.AccountName} logged in successfully");
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

                // Hash mật khẩu
                var hashedPassword = BC.HashPassword(request.Password);
                
                var accountRepository = _unitOfWork.GetRepository<Account>();
                var newAccount = _mapper.Map<Account>(request);
                newAccount.Password = hashedPassword;
                newAccount.CreatedAt = DateTime.UtcNow;
                newAccount.UpdatedAt = DateTime.UtcNow;
                newAccount.Status = true;

                await accountRepository.AddAsync(newAccount);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<UserResponseDTO>(newAccount);
                response.Token = _jwtService.GenerateToken(newAccount);

                _logger.LogInformation($"User {newAccount.AccountName} registered successfully");
                return response;
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

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                throw new ArgumentException("Username và password không được để trống");
            }

            if (!IsValidEmail(request.Email))
            {
                throw new ArgumentException("Email không hợp lệ");
            }

            var existingUsername = await accountRepository.GetAsync(u => 
                u.AccountName.ToLower() == request.Username.ToLower());
            if (existingUsername != null)
            {
                throw new InvalidOperationException("Username đã tồn tại");
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
