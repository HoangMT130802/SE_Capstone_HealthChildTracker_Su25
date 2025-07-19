using Contracts.DTOs.Survey;
using Repositories.Entities;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ISurveyService
    {
        Task<int> CreateSurveyAsync(CreateSurveyDto surveyDto, int accountId);
        Task<int> CreateSurveyQuestionAsync(int surveyId, CreateSurveyQuestionDto questionDto, int accountId);
        Task<QueryResultModel<IEnumerable<SurveyQuestionDto>>> GetQuestionsBySurveyIdAsync(int surveyId, int? pageIndex = null, int? pageSize = null);
        Task SubmitAppointmentSurveyAsync(int appointmentId, IEnumerable<AppointmentSurveyDto> answers, int accountId);
        Task<QueryResultModel<IEnumerable<SurveyResponseDto>>> GetSurveyResponsesByAppointmentIdAsync(int appointmentId, int? pageIndex = null, int? pageSize = null);
        Task DeleteSurveyAsync(int surveyId, int accountId);
        Task UpdateSurveyAsync(int surveyId, CreateSurveyDto surveyDto, int accountId);
        Task<QueryResultModel<IEnumerable<SurveyDto>>> GetAllSurveysAsync(int? pageIndex = null, int? pageSize = null);
        Task<QueryResultModel<IEnumerable<SurveyQuestionDto>>> GetAllQuestionsAsync(int? pageIndex = null, int? pageSize = null);
    }
}
