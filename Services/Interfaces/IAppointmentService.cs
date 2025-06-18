using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Appointment;

namespace BusinessLogic.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDTO>> GetUserAppointmentsAsync(int userId);
        Task<IEnumerable<AppointmentDTO>> GetDoctorAppointmentsAsync(int doctorId);
        Task<AppointmentDTO> GetAppointmentByIdAsync(int appointmentId);
        Task<AppointmentDTO> CreateAppointmentAsync(CreateAppointmentDTO appointmentDTO);
        Task<bool> CancelAppointmentAsync(int appointmentId, int userId);
        Task<AppointmentDTO> CompleteAppointmentAsync(int appointmentId, string note, int doctorId);
    }
} 