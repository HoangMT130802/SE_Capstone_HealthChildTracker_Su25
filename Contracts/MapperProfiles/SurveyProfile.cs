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

            CreateMap<SurveyQuestion, SurveyQuestionDto>();

            CreateMap<AppointmentSurveyDto, AppointmentSurvey>();

            CreateMap<AppointmentSurvey, SurveyResponseDto>()
                .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.Question.QuestionText))
                .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => GetAnswerText(src)))
                .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.CreatedAt));
            CreateMap<CreateSurveyQuestionDto, SurveyQuestion>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.AppointmentSurveys, opt => opt.MapFrom(src => new List<AppointmentSurvey>()))
            .ForMember(dest => dest.SurveyAnswers, opt => opt.MapFrom(src => new List<SurveyAnswer>()));
        }


        private string GetAnswerText(AppointmentSurvey src)
        {
            if (src.Answer != null && src.Answer.AnswerText != null)
                return src.Answer.AnswerText;
            return src.AnswerText ?? string.Empty;
        }
    }
}
