using Contracts.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
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
        public async Task<IActionResult> GetOrders([FromQuery] string status = null, [FromQuery] int? facilityId = null, [FromQuery] int? pageIndex = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var orders = await _orderService.GetOrdersAsync(status, facilityId, pageIndex, pageSize);
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
    }
}
