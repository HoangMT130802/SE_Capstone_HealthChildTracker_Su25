using AutoMapper;
using Services.Interfaces;
using Contracts.DTOs.GrowthRecord;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
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
        public async Task<GrowthRecordDTO> CreateGrowthRecordAsync(int childId, CreateGrowthRecordDTO recordDTO)
        {
            try
            {
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId);

                if (child == null)
                {
                    throw new KeyNotFoundException($"Child with ID {childId} not found");
                }

                var birthDate = child.BirthDate.Date;
                var createdAtDate = recordDTO.CreatedAt.Date;
                var currentDate = DateTime.UtcNow.Date;

                // Validate created date
                if (createdAtDate < birthDate)
                {
                    throw new InvalidOperationException($"Không thể tạo record trước ngày sinh của trẻ. Ngày sinh: {child.BirthDate:dd/MM/yyyy}");
                }

                if (createdAtDate > currentDate)
                {
                    throw new InvalidOperationException($"Không thể tạo record trong tương lai. Ngày tạo: {createdAtDate:dd/MM/yyyy}");
                }

                var currentDateTime = DateTime.UtcNow;

                var recordRepository = _unitOfWork.GetRepository<GrowthRecord>();
                var record = _mapper.Map<GrowthRecord>(recordDTO);

                record.ChildId = childId;

                decimal heightInMeters = recordDTO.Height / 100;
                record.Bmi = Math.Round(recordDTO.Weight / (heightInMeters * heightInMeters), 2);
                // CreatedAt đã được set từ DTO thông qua mapper
                record.UpdatedAt = currentDateTime;
                record.Note = recordDTO.Note;

                await recordRepository.AddAsync(record);
                await _unitOfWork.SaveChangesAsync();

                var savedRecord = await recordRepository.GetAsync(
                    r => r.RecordId == record.RecordId,
                    includeProperties: "Child"
                );

                return _mapper.Map<GrowthRecordDTO>(savedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating growth record for child {childId}");
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

                var birthDate = record.Child.BirthDate.Date;
                var createdAtDate = recordDTO.CreatedAt.Date;
                var currentDate = DateTime.UtcNow.Date;

                // Validate created date
                if (createdAtDate < birthDate)
                {
                    throw new InvalidOperationException($"Không thể cập nhật record trước ngày sinh của trẻ. Ngày sinh: {record.Child.BirthDate:dd/MM/yyyy}");
                }
                
                if (createdAtDate > currentDate)
                {
                    throw new InvalidOperationException($"Không thể cập nhật record trong tương lai. Ngày tạo: {createdAtDate:dd/MM/yyyy}");
                }

                var currentDateTime = DateTime.UtcNow;

                _mapper.Map(recordDTO, record);

                decimal heightInMeters = record.Height / 100;
                record.Bmi = Math.Round(record.Weight / (heightInMeters * heightInMeters), 2);

                record.UpdatedAt = currentDateTime;
                record.Note = recordDTO.Note;

                recordRepository.Update(record);
                await _unitOfWork.SaveChangesAsync();

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
