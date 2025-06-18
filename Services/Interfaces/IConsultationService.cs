using BusinessLogic.DTOs.ConsultationRequest;
using BusinessLogic.DTOs.ConsultationResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IConsultationService
    {
        // Các phương thức chính cho request
        Task<IEnumerable<ConsultationRequestDTO>> GetAllConsulationRequest();
        Task<ConsultationRequestDTO> CreateRequestAsync(int userId, CreateConsultationRequestDTO request);
        Task<ConsultationRequestDTO> GetRequestByIdAsync(int requestId, int userId);
        Task<IEnumerable<ConsultationRequestDTO>> GetUserRequestsAsync(int userId);
        Task<IEnumerable<ConsultationRequestDTO>> GetDoctorRequestsAsync(int doctorId);
        Task<IEnumerable<ConsultationResponseDTO>> GetDoctorResponsesAsync(int doctorId);

        // Phương thức xử lý câu hỏi và trả lời
        Task<ConsultationResponseDTO> AddResponseAsync(
            int requestId, 
            int userId, 
            AskQuestionDTO questionDto, 
            int? parentResponseId = null, 
            bool isFromDoctor = false);

        // Phương thức quản lý trạng thái request
        Task<ConsultationRequestDTO> UpdateRequestStatusAsync(
            int requestId, 
            int userId, 
            string action, 
            string reason = null, 
            bool? isSatisfied = null);

        // Phương thức hỗ trợ
        Task<ConsultationResponseDTO> UpdateResponseContentAsync(int responseId, string newContent);
        Task<ConsultationRequestDTO> AssignDoctorAsync(int requestId, int doctorId);
        Task<Dictionary<int, int>> GetDoctorWorkloadAsync();
        Task CheckAndUpdateExpiredRequestsAsync();
    }
}
