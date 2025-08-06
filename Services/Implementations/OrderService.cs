using AutoMapper;
using Contracts.DTOs.Order;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        private int GetCurrentAccountId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(accountIdClaim, out int accountId))
                {
                    return accountId;
                }
            }
            return 0;
        }


        public async Task<OrderDTO> CreatePackageOrderAsync(CreatePackageOrderDTO orderDto)
        {
            try
            {
                _logger.LogInformation($"Creating package order for PackageId: {orderDto.PackageId}");

                var accountId = GetCurrentAccountId();
                if (accountId == 0)
                {
                    throw new UnauthorizedAccessException("Không thể xác định AccountId của người dùng hiện tại từ token");
                }

                // Tra cứu Member dựa trên AccountId
                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == accountId);
                if (member == null)
                {
                    throw new UnauthorizedAccessException($"Không tìm thấy Member gắn với AccountId {accountId}. Chỉ thành viên mới có thể tạo đơn hàng.");
                }

                var memberId = member.MemberId;

                var packageRepository = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepository.GetAsync(
                    p => p.PackageId == orderDto.PackageId,
                    includeProperties: "PackageVaccines,PackageVaccines.Disease,PackageVaccines.FacilityVaccine"
                );
                if (package == null)
                {
                    throw new InvalidOperationException($"Package với ID {orderDto.PackageId} không tồn tại");
                }

                var order = _mapper.Map<Order>(orderDto);
                var currentTime = DateTime.UtcNow;
                order.OrderDate = orderDto.OrderDate != default ? orderDto.OrderDate : currentTime;
                order.CreatedAt = currentTime;
                order.UpdatedAt = currentTime;
                order.Status = orderDto.Status ?? "Pending";
                order.TotalAmount = 0;
                order.MemberId = memberId;
                order.Package = package;

                var orderDetailRepository = _unitOfWork.GetRepository<OrderDetail>();
                var selectedDiseaseIds = orderDto.SelectedVaccines.Select(v => v.DiseaseId).Distinct().ToList();
                var packageDiseaseIds = package.PackageVaccines.Select(pv => pv.DiseaseId).Distinct().ToList();

                if (selectedDiseaseIds.Except(packageDiseaseIds).Any())
                {
                    throw new InvalidOperationException("Một hoặc nhiều DiseaseId không thuộc gói vaccine này");
                }

                var facilityVaccineRepository = _unitOfWork.GetRepository<FacilityVaccine>();
                foreach (var selectedVaccine in orderDto.SelectedVaccines)
                {
                    var facilityVaccine = await facilityVaccineRepository.GetAsync(
                        fv => fv.FacilityVaccineId == selectedVaccine.FacilityVaccineId,
                        includeProperties: "Vaccine.VaccineDiseases"
                    );
                    if (facilityVaccine == null)
                    {
                        throw new InvalidOperationException($"FacilityVaccine với ID {selectedVaccine.FacilityVaccineId} không tồn tại");
                    }
                    if (facilityVaccine.AvailableQuantity < selectedVaccine.Quantity)
                    {
                        throw new InvalidOperationException($"Số lượng vaccine {selectedVaccine.FacilityVaccineId} không đủ, chỉ còn {facilityVaccine.AvailableQuantity}");
                    }

                    var diseaseMatch = facilityVaccine.Vaccine?.VaccineDiseases?.Any(vd => vd.DiseaseId == selectedVaccine.DiseaseId);
                    if (diseaseMatch == null || !diseaseMatch.Value)
                    {
                        throw new InvalidOperationException($"FacilityVaccine với ID {selectedVaccine.FacilityVaccineId} không phù hợp với DiseaseId {selectedVaccine.DiseaseId}");
                    }

                    var orderDetail = _mapper.Map<OrderDetail>(selectedVaccine);
                    orderDetail.OrderId = order.OrderId; // OrderId sẽ được gán sau khi lưu
                    orderDetail.Price = facilityVaccine.Price * selectedVaccine.Quantity;
                    orderDetail.CreatedAt = currentTime;
                    orderDetail.UpdatedAt = currentTime;
                    order.OrderDetails.Add(orderDetail);

                    order.TotalAmount += orderDetail.Price;
                    facilityVaccine.AvailableQuantity -= selectedVaccine.Quantity;
                    facilityVaccineRepository.Update(facilityVaccine);
                }

                var orderRepository = _unitOfWork.GetRepository<Order>();
                await orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Lấy lại order đã lưu để đảm bảo OrderId được gán
                var savedOrder = await orderRepository.GetAsync(
                    o => o.OrderId == order.OrderId,
                    includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
                );
                return _mapper.Map<OrderDTO>(savedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating package order for PackageId {orderDto?.PackageId}");
                throw;
            }
        }


        public async Task<QueryResultModel<IEnumerable<OrderDTO>>> GetOrdersAsync(string status = null, int? pageIndex = null, int? pageSize = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving orders with status: {status ?? "all"}");
                var orderRepository = _unitOfWork.GetRepository<Order>();
                Expression<Func<Order, bool>>? filter = null;
                if (!string.IsNullOrEmpty(status))
                {
                    filter = o => o.Status == status;
                }

                var result = await orderRepository.GetAllAsync(
                    filter: filter,
                    include: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease,Member,OrderDetails.FacilityVaccine.Vaccine", 
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var orderDtos = _mapper.Map<IEnumerable<OrderDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<OrderDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = orderDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving orders with status {status}");
                throw;
            }
        }

        public async Task<OrderDTO> GetOrderByIdAsync(int orderId)
        {
            try
            {
                _logger.LogInformation($"Retrieving order with ID: {orderId}");
                var orderRepository = _unitOfWork.GetRepository<Order>();
                var order = await orderRepository.GetAsync(
                    o => o.OrderId == orderId,
                    includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease,Member,OrderDetails.FacilityVaccine.Vaccine"
                );
                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }
                return _mapper.Map<OrderDTO>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order with ID {orderId}");
                throw;
            }
        }

        public async Task<OrderDTO> UpdateOrderAsync(int orderId, UpdateOrderDTO orderDto)
        {
            try
            {
                _logger.LogInformation($"Updating order with ID: {orderId}");
                var orderRepository = _unitOfWork.GetRepository<Order>();
                var order = await orderRepository.GetAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }

                order.OrderDate = orderDto.OrderDate != default ? orderDto.OrderDate : order.OrderDate;
                order.Status = orderDto.Status ?? order.Status;
                order.UpdatedAt = DateTime.UtcNow;

                orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync();

                var updatedOrder = await orderRepository.GetAsync(
                    o => o.OrderId == orderId,
                    includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
                );
                return _mapper.Map<OrderDTO>(updatedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order with ID {orderId}");
                throw;
            }
        }
        public async Task<QueryResultModel<IEnumerable<OrderDTO>>> GetMyOrdersAsync(string status = null, int accountId = 0, int? pageIndex = null, int? pageSize = null)
        {
            try
            {
                _logger.LogInformation($"Retrieving orders for AccountId: {accountId} with status: {status ?? "all"}");
                if (accountId == 0)
                {
                    throw new UnauthorizedAccessException("Không thể xác định AccountId của người dùng hiện tại");
                }

                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == accountId);
                if (member == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy Member gắn với AccountId {accountId}");
                }
                var memberId = member.MemberId;

                var orderRepository = _unitOfWork.GetRepository<Order>();
                Expression<Func<Order, bool>>? filter = null;
                if (!string.IsNullOrEmpty(status) || memberId != 0)
                {
                    filter = o => (string.IsNullOrEmpty(status) || o.Status == status) && o.MemberId == memberId;
                }

                var result = await orderRepository.GetAllAsync(
                    filter: filter,
                    include: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease,Member,OrderDetails.FacilityVaccine.Vaccine",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var orderDtos = _mapper.Map<IEnumerable<OrderDTO>>(result.Data);
                return new QueryResultModel<IEnumerable<OrderDTO>>
                {
                    TotalCount = result.TotalCount,
                    Data = orderDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting my orders for AccountId {accountId}");
                throw;
            }
        }
        public async Task DeleteOrderAsync(int orderId)
        {
            try
            {
                _logger.LogInformation($"Deleting order with ID: {orderId}");
                var orderRepository = _unitOfWork.GetRepository<Order>();
                var order = await orderRepository.GetAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }

                orderRepository.Delete(order);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting order with ID {orderId}");
                throw;
            }
        }
    }
}
