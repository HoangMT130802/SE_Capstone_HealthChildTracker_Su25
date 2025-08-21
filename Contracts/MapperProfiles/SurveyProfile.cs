using AutoMapper;
using Contracts.DTOs.Survey;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.MapperProfiles
{
    public class SurveyProfile : Profile
    {
        public SurveyProfile()
        {
            CreateMap<CreateSurveyDto, HealthSurvey>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SurveyQuestions, opt => opt.Ignore());

            CreateMap<SurveyQuestion, SurveyQuestionDto>()
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
                .ForMember(dest => dest.SurveyId, opt => opt.MapFrom(src => src.SurveyId))
                .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.QuestionText))
                .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.QuestionType))
                .ForMember(dest => dest.IsRequired, opt => opt.MapFrom(src => src.IsRequired));

            CreateMap<AppointmentSurveyDto, AppointmentSurvey>()
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
                .ForMember(dest => dest.AnswerId, opt => opt.MapFrom(src => src.AnswerId))
                .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText))
                .ForMember(dest => dest.TemperatureC, opt => opt.MapFrom(src => src.TemperatureC))
                .ForMember(dest => dest.HeartRateBpm, opt => opt.MapFrom(src => src.HeartRateBpm))
                .ForMember(dest => dest.SystolicBpmmHg, opt => opt.MapFrom(src => src.SystolicBpmmHg))
                .ForMember(dest => dest.DiastolicBpmmHg, opt => opt.MapFrom(src => src.DiastolicBpmmHg))
                .ForMember(dest => dest.OxygenSatPercent, opt => opt.MapFrom(src => src.OxygenSatPercent))
                .ForMember(dest => dest.DecisionNote, opt => opt.MapFrom(src => src.DecisionNote))
                .ForMember(dest => dest.ConsentObtained, opt => opt.MapFrom(src => src.ConsentObtained))
                .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
                .ForMember(dest => dest.AppointmentId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Answer, opt => opt.Ignore())
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Question, opt => opt.Ignore());

            CreateMap<AppointmentSurvey, SurveyResponseDto>()
                .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
                .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.Question.QuestionText))
                .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => GetAnswerText(src)))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TemperatureC, opt => opt.MapFrom(src => src.TemperatureC))
                .ForMember(dest => dest.HeartRateBpm, opt => opt.MapFrom(src => src.HeartRateBpm))
                .ForMember(dest => dest.SystolicBpmmHg, opt => opt.MapFrom(src => src.SystolicBpmmHg))
                .ForMember(dest => dest.DiastolicBpmmHg, opt => opt.MapFrom(src => src.DiastolicBpmmHg))
                .ForMember(dest => dest.OxygenSatPercent, opt => opt.MapFrom(src => src.OxygenSatPercent))
                .ForMember(dest => dest.DecisionNote, opt => opt.MapFrom(src => src.DecisionNote))
                .ForMember(dest => dest.ConsentObtained, opt => opt.MapFrom(src => src.ConsentObtained));
            CreateMap<CreateSurveyQuestionDto, SurveyQuestion>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.AppointmentSurveys, opt => opt.MapFrom(src => new List<AppointmentSurvey>()))
            .ForMember(dest => dest.SurveyAnswers, opt => opt.MapFrom(src => new List<SurveyAnswer>()));

            CreateMap<HealthSurvey, SurveyDto>()
            .ForMember(dest => dest.SurveyQuestions, opt => opt.MapFrom(src => src.SurveyQuestions));
        }


        private string GetAnswerText(AppointmentSurvey src)
        {
            if (src.Answer != null && src.Answer.AnswerText != null)
                return src.Answer.AnswerText;
            return src.AnswerText ?? string.Empty;
        }
    }
}
