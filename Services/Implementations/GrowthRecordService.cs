using AutoMapper;
using BusinessLogic.DTOs.GrowthRecord;
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
    public class GrowthRecordService : IGrowthRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GrowthRecordService> _logger;

        public GrowthRecordService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GrowthRecordService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<GrowthRecordDTO>> GetAllGrowthRecordsByChildIdAsync(int childId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var records = await recordRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child");
                return _mapper.Map<IEnumerable<GrowthRecordDTO>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting growth records for child {childId}");
                throw;
            }
        }

        public async Task<GrowthRecordDTO> GetGrowthRecordByIdAsync(int recordId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var record = await recordRepository.GetAsync(r => r.RecordId == recordId, includeProperties: "Child");

                if (record == null)
                {
                    throw new KeyNotFoundException($"Growth record with ID {recordId} not found");
                }

                return _mapper.Map<GrowthRecordDTO>(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting growth record {recordId}");
                throw;
            }
        }
        public async Task<GrowthRecordDTO> CreateGrowthRecordAsync(CreateGrowthRecordDTO recordDTO)
        {
            try
            {
                // Validate child exists
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == recordDTO.ChildId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {recordDTO.ChildId} not found");
                }

                // Validate ngày sinh của trẻ
                var birthDate = child.BirthDate.Date;
                var currentDate = DateTime.UtcNow.Date;

                if (currentDate < birthDate)
                {
                    throw new InvalidOperationException($"Không thể tạo record trước ngày sinh của trẻ. Ngày sinh: {child.BirthDate:dd/MM/yyyy}");
                }

                // Kiểm tra UpdatedAt mới có vượt quá currentDate không
                var newUpdatedAt = DateTime.UtcNow;
                if (newUpdatedAt.Date > currentDate)
                {
                    throw new InvalidOperationException($"Không thể tạo record trong tương lai. Ngày tạo: {newUpdatedAt:dd/MM/yyyy}");
                }

                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var record = _mapper.Map<GrowthRecord>(recordDTO);

                // Calculate BMI: weight (kg) / (height (m) * height (m))
                // Convert height from cm to m
                decimal heightInMeters = recordDTO.Height / 100;
                record.Bmi = Math.Round(recordDTO.Weight / (heightInMeters * heightInMeters), 2);
                record.UpdatedAt = newUpdatedAt;
                record.Note = recordDTO.Note;

                await recordRepository.AddAsync(record);
                await _unitOfWork.SaveChangesAsync();

                // Lấy lại record với Child để tính tuổi
                var savedRecord = await recordRepository.GetAsync(
                    r => r.RecordId == record.RecordId,
                    includeProperties: "Child"
                );

                return _mapper.Map<GrowthRecordDTO>(savedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating growth record for child {recordDTO.ChildId}");
                throw;
            }
        }

        public async Task<GrowthRecordDTO> UpdateGrowthRecordAsync(int recordId, UpdateGrowthRecordDTO recordDTO)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var record = await recordRepository.GetAsync(
                    r => r.RecordId == recordId,
                    includeProperties: "Child"
                );

                if (record == null)
                {
                    throw new KeyNotFoundException($"Growth record with ID {recordId} not found");
                }

                // Validate ngày sinh của trẻ
                var birthDate = record.Child.BirthDate.Date;
                var currentDate = DateTime.UtcNow.Date;

                if (currentDate < birthDate)
                {
                    throw new InvalidOperationException($"Không thể cập nhật record trước ngày sinh của trẻ. Ngày sinh: {record.Child.BirthDate:dd/MM/yyyy}");
                }

                // Kiểm tra UpdatedAt mới có vượt quá currentDate không
                var newUpdatedAt = DateTime.UtcNow;
                if (newUpdatedAt.Date > currentDate)
                {
                    throw new InvalidOperationException($"Không thể cập nhật record trong tương lai. Ngày cập nhật: {newUpdatedAt:dd/MM/yyyy}");
                }

                _mapper.Map(recordDTO, record);

                // Recalculate BMI
                decimal heightInMeters = record.Height / 100;
                record.Bmi = Math.Round(record.Weight / (heightInMeters * heightInMeters), 2);

                record.UpdatedAt = newUpdatedAt;
                record.Note = recordDTO.Note;

                recordRepository.Update(record);
                await _unitOfWork.SaveChangesAsync();

                // Lấy lại record với Child để tính tuổi
                var updatedRecord = await recordRepository.GetAsync(
                    r => r.RecordId == recordId,
                    includeProperties: "Child"
                );

                return _mapper.Map<GrowthRecordDTO>(updatedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating growth record {recordId}");
                throw;
            }
        }

        public async Task<bool> DeleteGrowthRecordAsync(int recordId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var record = await recordRepository.GetAsync(r => r.RecordId == recordId);

                if (record == null)
                {
                    throw new KeyNotFoundException($"Growth record with ID {recordId} not found");
                }

                recordRepository.Delete(record);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting growth record {recordId}");
                throw;
            }
        }
    }
}
