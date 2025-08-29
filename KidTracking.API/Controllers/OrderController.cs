using Contracts.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUnitOfWork _unitOfWork;
        public OrderController(IOrderService orderService, IUnitOfWork unitOfWork)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }
        private async Task<bool> IsManager()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
            {
                return false;
            }

            var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Staff");
            return staff != null;
        }

        [HttpPost("package")]
        [Authorize]
        public async Task<IActionResult> CreatePackageOrder([FromBody] CreatePackageOrderDTO orderDto)
        {
            if (orderDto == null)
            {
                return BadRequest("Order data is required");
            }

            try
            {
                var order = await _orderService.CreatePackageOrderAsync(orderDto);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the order");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string status = null, [FromQuery] int? facilityId = null, [FromQuery] DateTime? orderDate = null, [FromQuery] int? pageIndex = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var orders = await _orderService.GetOrdersAsync(status, facilityId, orderDate, pageIndex, pageSize);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders([FromQuery] string status = null, [FromQuery] int? pageIndex = 1, [FromQuery] int? pageSize = 10)
        {
            if (pageIndex <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "PageIndex and PageSize must be positive" });
            }

            try
            {
                var accountId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;
                if (accountId == 0)
                {
                    throw new UnauthorizedAccessException("Không thể xác định AccountId của người dùng hiện tại");
                }

                var orders = await _orderService.GetMyOrdersAsync(status, accountId, pageIndex, pageSize);
                return Ok(new
                {
                    totalCount = orders.TotalCount,
                    pageIndex,
                    pageSize,
                    data = orders.Data
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDTO orderDto)
        {
            if (orderDto == null)
            {
                return BadRequest("Order data is required");
            }

            try
            {
                var order = await _orderService.UpdateOrderAsync(id, orderDto);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the order");
            }
        }

        [HttpDelete("{id}")]    
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the order");
            }
        }
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Staff mới có quyền thực hiện hành động này" });
                }
                var order = await _orderService.CancelOrderAsync(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi hủy đơn hàng" });
            }
        }
    }
}
