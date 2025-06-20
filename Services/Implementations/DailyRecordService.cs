using AutoMapper;
using Contracts.DTOs.DailyRecord;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class DailyRecordService : IDailyRecordService
    {
        private readonly IUnitOfWork _unitOfWork; private readonly IMapper _mapper; private readonly ILogger _logger;
        public DailyRecordService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DailyRecordService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<DailyRecordDTO>> GetAllDailyRecordsByChildIdAsync(int childId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var records = await recordRepository.FindAsync(r => r.ChildId == childId, includeProperties: "Child");
                return _mapper.Map<IEnumerable<DailyRecordDTO>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting daily records for child {childId}");
                throw;
            }
        }

        public async Task<DailyRecordDTO> GetDailyRecordByIdAsync(int recordId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var record = await recordRepository.GetAsync(r => r.DailyRecordId == recordId, includeProperties: "Child");

                if (record == null)
                {
                    throw new KeyNotFoundException($"Daily record with ID {recordId} not found");
                }

                return _mapper.Map<DailyRecordDTO>(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting daily record {recordId}");
                throw;
            }
        }

        public async Task<DailyRecordDTO> CreateDailyRecordAsync(CreateDailyRecordDTO recordDTO)
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

                // Validate record date
                var birthDate = child.BirthDate.Date;
                var recordDate = recordDTO.RecordDate.ToDateTime(TimeOnly.MinValue);

                if (recordDate < birthDate)
                {
                    throw new InvalidOperationException($"Cannot create record before child's birth date. Birth date: {child.BirthDate:dd/MM/yyyy}");
                }

                // Validate record date is not in the future
                var currentDate = DateTime.UtcNow.Date;
                if (recordDate.Date > currentDate)
                {
                    throw new InvalidOperationException($"Cannot create record in the future. Record date: {recordDate:dd/MM/yyyy}");
                }

                // Validate no existing record for the same child and date
                var recordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var existingRecord = await recordRepository.GetAsync(r => r.ChildId == recordDTO.ChildId && r.RecordDate == recordDTO.RecordDate);

                if (existingRecord != null)
                {
                    throw new InvalidOperationException($"A daily record already exists for child ID {recordDTO.ChildId} on date {recordDTO.RecordDate:dd/MM/yyyy}");
                }

                // Map and create new record
                var record = _mapper.Map<DailyRecord>(recordDTO);
                record.CreatedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;

                await recordRepository.AddAsync(record);
                await _unitOfWork.SaveChangesAsync();

                // Fetch saved record with Child
                var savedRecord = await recordRepository.GetAsync(
                    r => r.DailyRecordId == record.DailyRecordId,
                    includeProperties: "Child"
                );

                return _mapper.Map<DailyRecordDTO>(savedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating daily record for child {recordDTO.ChildId}");
                throw;
            }
        }

        public async Task<DailyRecordDTO> UpdateDailyRecordAsync(int recordId, UpdateDailyRecordDTO recordDTO)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var record = await recordRepository.GetAsync(
                    r => r.DailyRecordId == recordId,
                    includeProperties: "Child"
                );

                if (record == null)
                {
                    throw new KeyNotFoundException($"Daily record with ID {recordId} not found");
                }

                // Validate record date
                var birthDate = record.Child.BirthDate.Date;
                var recordDate = recordDTO.RecordDate.ToDateTime(TimeOnly.MinValue);

                if (recordDate < birthDate)
                {
                    throw new InvalidOperationException($"Cannot update record before child's birth date. Birth date: {record.Child.BirthDate:dd/MM/yyyy}");
                }

                // Validate record date is not in the future
                var currentDate = DateTime.UtcNow.Date;
                if (recordDate.Date > currentDate)
                {
                    throw new InvalidOperationException($"Cannot update record in the future. Record date: {recordDate:dd/MM/yyyy}");
                }

                _mapper.Map(recordDTO, record);
                record.UpdatedAt = DateTime.UtcNow;

                recordRepository.Update(record);
                await _unitOfWork.SaveChangesAsync();

                // Fetch updated record with Child
                var updatedRecord = await recordRepository.GetAsync(
                    r => r.DailyRecordId == recordId,
                    includeProperties: "Child"
                );

                return _mapper.Map<DailyRecordDTO>(updatedRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating daily record {recordId}");
                throw;
            }
        }

        public async Task<bool> DeleteDailyRecordAsync(int recordId)
        {
            try
            {
                var recordRepository = _unitOfWork.GetRepository<DailyRecord>();
                var record = await recordRepository.GetAsync(r => r.DailyRecordId == recordId);

                if (record == null)
                {
                    throw new KeyNotFoundException($"Daily record with ID {recordId} not found");
                }

                recordRepository.Delete(record);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting daily record {recordId}");
                throw;
            }
        }
    }
}
