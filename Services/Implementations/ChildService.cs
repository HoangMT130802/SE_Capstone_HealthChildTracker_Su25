using AutoMapper;
using BusinessLogic.DTOs.Children;
using BusinessLogic.Services.Interfaces;
using DataAccess.Entities;
using DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Implementations
{
    public class ChildService : IChildService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ChildService> _logger;

        public ChildService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ChildService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ChildDTO>> GetAllChildrenByUserIdAsync(int userId)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var children = await childRepository.FindAsync(c => c.UserId == userId);
                return _mapper.Map<IEnumerable<ChildDTO>>(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting children for user {userId}");
                throw;
            }
        }

        public async Task<ChildDTO> GetChildByIdAsync(int childId, int userId)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.UserId == userId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for user {userId}");
                }

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting child {childId} for user {userId}");
                throw;
            }
        }

        public async Task<ChildDTO> CreateChildAsync(int userId, CreateChildDTO childDTO)
        {
            try
            {
                // Kiểm tra số lượng trẻ tối đa theo gói membership
                var userMembershipRepo = _unitOfWork.GetRepository<UserMembership>();
                var activeMembership = await userMembershipRepo.GetAsync(
                    um => um.UserId == userId &&
                          um.Status == "Active" &&
                          um.EndDate > DateTime.UtcNow,
                    includeProperties: "Membership"
                );

                if (activeMembership == null)
                {
                    throw new InvalidOperationException("Bạn cần có gói membership active để thêm trẻ");
                }

                // Kiểm tra số lượng trẻ hiện tại
                var childRepository = _unitOfWork.GetRepository<Child>();
                var currentChildrenCount = await childRepository.CountAsync(c => c.UserId == userId && c.Status == true);

                if (currentChildrenCount >= activeMembership.Membership.MaxChildren)
                {
                    throw new InvalidOperationException($"Bạn đã đạt giới hạn số lượng trẻ ({activeMembership.Membership.MaxChildren}) theo gói membership");
                }

                var child = _mapper.Map<Child>(childDTO);
                child.UserId = userId;
                child.Status = true;
                child.CreatedAt = DateTime.UtcNow;
                child.UpdateAt = DateTime.UtcNow;

                await childRepository.AddAsync(child);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating child for user {userId}");
                throw;
            }
        }
        public async Task<ChildDTO> UpdateChildAsync(int childId, int userId, UpdateChildDTO childDTO)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.UserId == userId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for user {userId}");
                }

                _mapper.Map(childDTO, child);
                child.UpdateAt = DateTime.UtcNow;

                childRepository.Update(child);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<ChildDTO>(child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating child {childId} for user {userId}");
                throw;
            }
        }

        public async Task<bool> SoftDeleteChildAsync(int childId, int userId)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.UserId == userId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for user {userId}");
                }

                // Soft delete
                child.Status = false;
                child.UpdateAt = DateTime.UtcNow;

                childRepository.Update(child);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting child {childId} for user {userId}");
                throw;
            }
        }
        public async Task<bool> HardDeleteChildAsync(int childId, int userId)
        {
            try
            {            
                var childRepository = _unitOfWork.GetRepository<Child>();
             
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.UserId == userId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found for user {userId}");
                }

                // Xóa các bản ghi liên quan trước
                

           /*     // Xóa GrowthRecords
                var growthRecordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var growthRecords = await growthRecordRepository.GetAllAsync(gr => gr.ChildId == childId);
                foreach (var record in growthRecords)
                {
                    growthRecordRepository.Delete(record);
                }

                // Xóa DailyRecords
                var dailyRecordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var dailyRecords = await dailyRecordRepository.GetAllAsync(dr => dr.ChildId == childId);
                foreach (var record in dailyRecords)
                {
                    dailyRecordRepository.Delete(record);
                }

                // Xóa ConsultationRequests và ConsultationResponses liên quan
                var consultationRequestRepository = _unitOfWork.GetRepository<ConsultationRequest>();
                var consultationRequests = await consultationRequestRepository.GetAllAsync(cr => cr.ChildId == childId);

                var consultationResponseRepository = _unitOfWork.GetRepository<ConsultationResponse>();
                foreach (var request in consultationRequests)
                {
                    var responses = await consultationResponseRepository.GetAllAsync(cr => cr.RequestId == request.RequestId);
                    foreach (var response in responses)
                    {
                        consultationResponseRepository.Delete(response);
                    }
                    consultationRequestRepository.Delete(request);
                }

                // Xóa Appointments và Ratings liên quan
                var appointmentRepository = _unitOfWork.GetRepository<Appointment>();
                var appointments = await appointmentRepository.GetAllAsync(a => a.ChildId == childId);

                var ratingRepository = _unitOfWork.GetRepository<Rating>();
                foreach (var appointment in appointments)
                {
                    var ratings = await ratingRepository.GetAllAsync(r => r.AppointmentId == appointment.AppointmentId);
                    foreach (var rating in ratings)
                    {
                        ratingRepository.Delete(rating);
                    }
                    appointmentRepository.Delete(appointment);
                }*/

               
                childRepository.Delete(child);

                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting child {childId} for user {userId}");
                throw;
            }
        }
    }
}
