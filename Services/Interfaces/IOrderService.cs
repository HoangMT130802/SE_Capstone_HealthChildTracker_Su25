using Contracts.DTOs.Order;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDTO> CreatePackageOrderAsync(CreatePackageOrderDTO orderDto);
        Task<QueryResultModel<IEnumerable<OrderDTO>>> GetOrdersAsync(string status = null, int? pageIndex = null, int? pageSize = null);
        Task<OrderDTO> GetOrderByIdAsync(int orderId);
        Task<OrderDTO> UpdateOrderAsync(int orderId, UpdateOrderDTO orderDto);
        Task DeleteOrderAsync(int orderId);
        Task<QueryResultModel<IEnumerable<OrderDTO>>> GetMyOrdersAsync(string status = null, int accountId = 0, int? pageIndex = null, int? pageSize = null);
    }
}
