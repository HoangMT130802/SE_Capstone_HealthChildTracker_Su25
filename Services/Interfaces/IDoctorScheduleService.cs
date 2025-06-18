using BusinessLogic.DTOs.Doctor_Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IDoctorScheduleService
    {
        // Lấy danh sách 8 slot mặc định
        List<TimeSlotDTO> GetDefaultTimeSlots();
        
        // Tạo lịch làm việc mới
        Task<DoctorScheduleDTO> CreateScheduleAsync(CreateDoctorScheduleDTO scheduleDTO);
        
        // Lấy lịch làm việc theo tuần (từ thứ 2 đến thứ 6)
        Task<IEnumerable<DoctorScheduleDTO>> GetDoctorSchedulesByWeekAsync(int doctorId, DateOnly weekStart);
        
        // Lấy các slot có sẵn của một lịch
        Task<IEnumerable<TimeSlotDTO>> GetAvailableSlotsAsync(int scheduleId);
    }
}
