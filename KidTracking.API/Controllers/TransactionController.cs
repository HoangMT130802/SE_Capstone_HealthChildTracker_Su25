using Contracts.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionController(
            ITransactionService transactionService,
            ILogger<TransactionController> logger,
            IUnitOfWork unitOfWork)
        {
            _transactionService = transactionService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Helper Methods
        private async Task<int?> GetCurrentAccountId()
        {
            try
            {
                var currentAccountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(currentAccountIdClaim) || !int.TryParse(currentAccountIdClaim, out int currentAccountId))
                {
                    return null;
                }
                return currentAccountId;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> ValidateAdminAccess()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue) return false;

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == accountId.Value);
                
                return account != null && account.Role == "Admin";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateAdminAccess");
                return false;
            }
        }

        private async Task<int?> GetFacilityIdForStaff()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue) return null;

                var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await facilityStaffRepo.GetAsync(s => s.AccountId == accountId.Value);
                
                return staff?.FacilityId;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Lấy tất cả transactions (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetAllTransactions()
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem tất cả transactions");
                }

                var transactions = await _transactionService.GetAllTransactionsAsync();
                return Ok(new
                {
                    success = true,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả transactions");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách giao dịch"
                });
            }
        }

        /// <summary>
        /// Lấy transactions của người dùng hiện tại
        /// </summary>
        [HttpGet("my-transactions")]
        [Authorize(Roles = "Member")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetMyTransactions()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định AccountId từ token");
                }

                var transactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId.Value);
                return Ok(new
                {
                    success = true,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transactions của user");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách giao dịch"
                });
            }
        }

        /// <summary>
        /// Lấy transactions theo AccountId (Admin only)
        /// </summary>
        [HttpGet("account/{accountId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetTransactionsByAccountId(int accountId)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem transactions của tài khoản khác");
                }

                var transactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId);
                return Ok(new
                {
                    success = true,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transactions cho AccountId: {AccountId}", accountId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách giao dịch"
                });
            }
        }

        /// <summary>
        /// Lấy transactions của facility (cho FacilityStaff)
        /// </summary>
        [HttpGet("facility/my-transactions")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetMyFacilityTransactions()
        {
            try
            {
                var facilityId = await GetFacilityIdForStaff();
                if (!facilityId.HasValue)
                {
                    return Forbid("Không thể xác định FacilityId cho staff");
                }

                var transactions = await _transactionService.GetTransactionsByFacilityIdAsync(facilityId.Value);
                return Ok(new
                {
                    success = true,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy facility transactions cho staff");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách giao dịch facility"
                });
            }
        }

        /// <summary>
        /// Lấy transactions theo FacilityId (Admin only)
        /// </summary>
        [HttpGet("facility/{facilityId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetTransactionsByFacilityId(int facilityId)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem transactions của facility khác");
                }

                var transactions = await _transactionService.GetTransactionsByFacilityIdAsync(facilityId);
                return Ok(new
                {
                    success = true,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transactions cho FacilityId: {FacilityId}", facilityId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách giao dịch facility"
                });
            }
        }

        /// <summary>
        /// Lấy transaction theo ID
        /// </summary>
        [HttpGet("{transactionId}")]
        public async Task<ActionResult<TransactionDTO>> GetTransactionById(int transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
                
                // Kiểm tra quyền truy cập
                var accountId = await GetCurrentAccountId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (userRole == "Admin")
                {
                    // Admin có thể xem tất cả
                    return Ok(new { success = true, data = transaction });
                }
                else if (userRole == "Member")
                {
                    // Member chỉ xem được transaction của mình
                    if (transaction.UserMembership?.AccountId == accountId)
                    {
                        return Ok(new { success = true, data = transaction });
                    }
                }
                else if (userRole == "FacilityStaff")
                {
                    // FacilityStaff chỉ xem được transaction của facility mình
                    var facilityId = await GetFacilityIdForStaff();
                    if (transaction.FacilityMembershipSubscription?.FacilityId == facilityId)
                    {
                        return Ok(new { success = true, data = transaction });
                    }
                }

                return Forbid("Không có quyền xem transaction này");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy transaction với ID: {TransactionId}", transactionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy thông tin giao dịch"
                });
            }
        }

        /// <summary>
        /// Tạo transaction mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TransactionDTO>> CreateTransaction([FromBody] CreateTransactionDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền tạo transaction");
                }

                var transaction = await _transactionService.CreateTransactionAsync(createDto);
                return CreatedAtAction(nameof(GetTransactionById), new { transactionId = transaction.TransactionId }, 
                    new { success = true, data = transaction });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo transaction");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi tạo giao dịch"
                });
            }
        }
    }
} 