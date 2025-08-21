using AutoMapper;
using Contracts.DTOs.Survey;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Models.QueryModels;
using Services.Interfaces;

public class SurveyService : ISurveyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<SurveyService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task ValidateDoctorAccess(int accountId)
    {
        var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
        var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Doctor");
        if (staff == null)
        {
            throw new UnauthorizedAccessException($"User with AccountId {accountId} is not a Doctor or does not belong to Facility");
        }
    }
    public async Task<int> CreateSurveyQuestionAsync(int surveyId, CreateSurveyQuestionDto questionDto, int accountId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(questionDto.QuestionText))
                throw new ArgumentException("Question text is required");

            if (string.IsNullOrWhiteSpace(questionDto.QuestionType) || !new[] { "Text", "MultipleChoice", "YesNo" }.Contains(questionDto.QuestionType))
                throw new ArgumentException("Question type must be 'Text', 'MultipleChoice', or 'YesNo'");

            var surveyRepository = _unitOfWork.GetRepository<HealthSurvey>();
            var survey = await surveyRepository.GetAsync(s => s.SurveyId == surveyId);
            if (survey == null)
                throw new KeyNotFoundException($"Survey with ID {surveyId} not found");

            await ValidateDoctorAccess(accountId);

            var question = new SurveyQuestion
            {
                SurveyId = surveyId,
                QuestionText = questionDto.QuestionText,
                QuestionType = questionDto.QuestionType,
                IsRequired = questionDto.IsRequired,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AppointmentSurveys = new List<AppointmentSurvey>(),
                SurveyAnswers = new List<SurveyAnswer>()
            };

            var questionRepository = _unitOfWork.GetRepository<SurveyQuestion>();
            await questionRepository.AddAsync(question);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created survey question with ID {question.QuestionId} by AccountId {accountId}");
            return question.QuestionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating survey question for SurveyId {surveyId} by AccountId {accountId}");
            throw;
        }
    }

    public async Task<int> CreateSurveyAsync(CreateSurveyDto surveyDto, int accountId)
    {
        try
        {
            if (surveyDto.EndDate < surveyDto.StartDate)
                throw new ArgumentException("End date must be after start date");

            var survey = _mapper.Map<HealthSurvey>(surveyDto);
            survey.CreatedAt = DateTime.UtcNow;
            survey.UpdatedAt = DateTime.UtcNow;

            var surveyRepository = _unitOfWork.GetRepository<HealthSurvey>();
            await surveyRepository.AddAsync(survey);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created survey with ID {survey.SurveyId} by AccountId {accountId}");
            return survey.SurveyId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating survey by AccountId {accountId}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<SurveyQuestionDto>>> GetQuestionsBySurveyIdAsync(int surveyId, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var questionRepository = _unitOfWork.GetRepository<SurveyQuestion>();
            var questions = await questionRepository.GetAllAsync(
                filter: q => q.SurveyId == surveyId,
                orderBy: q => q.OrderBy(q => q.QuestionId),
                include: "Survey",
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var questionDtos = _mapper.Map<IEnumerable<SurveyQuestionDto>>(questions.Data);
            return new QueryResultModel<IEnumerable<SurveyQuestionDto>>
            {
                TotalCount = questions.TotalCount,
                Data = questionDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting questions for survey ID {surveyId}");
            throw;
        }
    }

    public async Task SubmitAppointmentSurveyAsync(int appointmentId, IEnumerable<AppointmentSurveyDto> answers, int accountId)
    {
        try
        {
            // Validate Doctor role trước khi submit survey
            await ValidateDoctorAccess(accountId);

            var appointmentRepository = _unitOfWork.GetRepository<VaccinationAppointment>();
            var appointment = await appointmentRepository.GetAsync(a => a.AppointmentId == appointmentId, includeProperties: "Schedule.Facility");
            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found");

            var facilityId = appointment.Schedule?.FacilityId ?? throw new InvalidOperationException($"Appointment with ID {appointmentId} has no associated facility");
            var appointmentSurveyRepository = _unitOfWork.GetRepository<AppointmentSurvey>();
            var currentTime = DateTime.UtcNow;

            foreach (var answerDto in answers)
            {
                var appointmentSurvey = _mapper.Map<AppointmentSurvey>(answerDto);
                appointmentSurvey.AppointmentId = appointmentId;
                appointmentSurvey.CreatedAt = currentTime;
                appointmentSurvey.UpdatedAt = currentTime;
                await appointmentSurveyRepository.AddAsync(appointmentSurvey);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Submitted survey for appointment ID {appointmentId} by Doctor AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error submitting survey for appointment ID {appointmentId} by AccountId {accountId}");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<SurveyResponseDto>>> GetSurveyResponsesByAppointmentIdAsync(int appointmentId, int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var appointmentSurveyRepository = _unitOfWork.GetRepository<AppointmentSurvey>();
            var responses = await appointmentSurveyRepository.GetAllAsync(
                filter: asr => asr.AppointmentId == appointmentId,
                orderBy: asr => asr.OrderBy(asr => asr.QuestionId),
                include: "Question,Answer",
                pageIndex: pageIndex,
                pageSize: pageSize
            );

            var responseDtos = _mapper.Map<IEnumerable<SurveyResponseDto>>(responses.Data);
            return new QueryResultModel<IEnumerable<SurveyResponseDto>>
            {
                TotalCount = responses.TotalCount,
                Data = responseDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting survey responses for appointment ID {appointmentId}");
            throw;
        }
    }

    public async Task DeleteSurveyAsync(int surveyId, int accountId)
    {
        try
        {
            var surveyRepository = _unitOfWork.GetRepository<HealthSurvey>();
            var survey = await surveyRepository.GetAsync(s => s.SurveyId == surveyId);
            if (survey == null)
                throw new KeyNotFoundException($"Survey with ID {surveyId} not found");

            await ValidateDoctorAccess(accountId);

            surveyRepository.Delete(survey);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Deleted survey with ID {surveyId} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting survey with ID {surveyId} by AccountId {accountId}");
            throw;
        }
    }

    public async Task UpdateSurveyAsync(int surveyId, CreateSurveyDto surveyDto, int accountId)
    {
        try
        {
            if (surveyDto.EndDate < surveyDto.StartDate)
                throw new ArgumentException("End date must be after start date");

            var surveyRepository = _unitOfWork.GetRepository<HealthSurvey>();
            var survey = await surveyRepository.GetAsync(s => s.SurveyId == surveyId);
            if (survey == null)
                throw new KeyNotFoundException($"Survey with ID {surveyId} not found");

            await ValidateDoctorAccess(accountId);

            _mapper.Map(surveyDto, survey);
            survey.UpdatedAt = DateTime.UtcNow;

            surveyRepository.Update(survey);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Updated survey with ID {surveyId} by AccountId {accountId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating survey with ID {surveyId} by AccountId {accountId}");
            throw;
        }
    }
    public async Task<QueryResultModel<IEnumerable<SurveyDto>>> GetAllSurveysAsync(int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var surveyRepository = _unitOfWork.GetRepository<HealthSurvey>();
            var surveys = await surveyRepository.GetAllAsync(
                orderBy: s => s.OrderBy(s => s.SurveyId),
                include: "SurveyQuestions", 
                pageIndex: pageIndex,
                pageSize: pageSize
            );

            var surveyDtos = _mapper.Map<IEnumerable<SurveyDto>>(surveys.Data);
            return new QueryResultModel<IEnumerable<SurveyDto>>
            {
                TotalCount = surveys.TotalCount,
                Data = surveyDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all surveys");
            throw;
        }
    }

    public async Task<QueryResultModel<IEnumerable<SurveyQuestionDto>>> GetAllQuestionsAsync(int? pageIndex = null, int? pageSize = null)
    {
        try
        {
            var questionRepository = _unitOfWork.GetRepository<SurveyQuestion>();
            var questions = await questionRepository.GetAllAsync(
                orderBy: q => q.OrderBy(q => q.QuestionId),
                include: "Survey", 
                pageIndex: pageIndex,
                pageSize: pageSize
            );
            var questionDtos = _mapper.Map<IEnumerable<SurveyQuestionDto>>(questions.Data);
            return new QueryResultModel<IEnumerable<SurveyQuestionDto>>
            {
                TotalCount = questions.TotalCount,
                Data = questionDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all questions");
            throw;
        }
    }
}