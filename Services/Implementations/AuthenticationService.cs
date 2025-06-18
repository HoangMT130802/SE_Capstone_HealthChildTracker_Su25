using AutoMapper;
using BusinessLogic.DTOs.Authentication;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace BusinessLogic.Services.Implementations
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
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    throw new ArgumentException("Username và mật khẩu không được để trống");
                }

                var userRepository = _unitOfWork.GetRepository<User>();
                var user = await userRepository.GetAsync(u =>
                    (u.Username.ToLower() == request.Username.ToLower() ||
                     u.Email.ToLower() == request.Username.ToLower()));

                if (user == null || !BC.Verify(request.Password, user.Password))
                {
                    throw new UnauthorizedAccessException("Thông tin đăng nhập không chính xác");
                }

                if (!user.Status)
                {
                    throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa");
                }

                var response = _mapper.Map<UserResponseDTO>(user);
                response.Token = _jwtService.GenerateToken(user);

                _logger.LogInformation($"User {user.Username} logged in successfully");
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
                
                var userRepository = _unitOfWork.GetRepository<User>();
                var newUser = _mapper.Map<User>(request);
                newUser.Password = hashedPassword;
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.UpdatedAt = DateTime.UtcNow;
                newUser.Status = true;

                await userRepository.AddAsync(newUser);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<UserResponseDTO>(newUser);
                response.Token = _jwtService.GenerateToken(newUser);

                _logger.LogInformation($"User {newUser.Username} registered successfully");
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
            var userRepository = _unitOfWork.GetRepository<User>();

            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                throw new ArgumentException("Username và password không được để trống");
            }

            if (!IsValidEmail(request.Email))
            {
                throw new ArgumentException("Email không hợp lệ");
            }

            var existingUsername = await userRepository.GetAsync(u => 
                u.Username.ToLower() == request.Username.ToLower());
            if (existingUsername != null)
            {
                throw new InvalidOperationException("Username đã tồn tại");
            }

            var existingEmail = await userRepository.GetAsync(u => 
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
