using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Services.Implementations
{
    public class AIRecommendationService : IAIRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIRecommendationService> _logger;

        public AIRecommendationService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<AIRecommendationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateGrowthRecommendationsAsync(
            GrowthAssessmentContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🚀 AIRecommendationService: Bắt đầu tạo growth recommendations cho trẻ {ChildName}", context.Child.FullName);
                
                var prompt = BuildGrowthPredictionPrompt(context);
                _logger.LogInformation("📝 Prompt đã tạo, độ dài: {PromptLength} ký tự", prompt.Length);
                
                var response = await CallAIAPI(prompt, cancellationToken);
                _logger.LogInformation("✅ AI đã trả về response, độ dài: {ResponseLength} ký tự", response?.Length ?? 0);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AIRecommendationService: Lỗi khi tạo AI recommendations cho growth prediction. Lỗi: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<string> GenerateBasicAssessmentRecommendationsAsync(
            BasicAssessmentContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🚀 AIRecommendationService: Bắt đầu tạo basic assessment recommendations cho trẻ {ChildName}", context.Child.FullName);
                
                var prompt = BuildBasicAssessmentPrompt(context);
                _logger.LogInformation("📝 Basic Assessment Prompt đã tạo, độ dài: {PromptLength} ký tự", prompt.Length);
                
                var response = await CallAIAPI(prompt, cancellationToken);
                _logger.LogInformation("✅ AI đã trả về basic assessment response, độ dài: {ResponseLength} ký tự", response?.Length ?? 0);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AIRecommendationService: Lỗi khi tạo AI recommendations cho basic assessment. Lỗi: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private async Task<string> CallAIAPI(string prompt, CancellationToken cancellationToken)
        {
            var provider = _configuration["AI:Provider"] ?? "OpenAI";
            _logger.LogInformation("🔧 Sử dụng AI Provider: {Provider}", provider);
            
            return provider switch
            {
                "OpenAI" => await CallOpenAIAsync(prompt, cancellationToken),
                "Claude" => await CallClaudeAsync(prompt, cancellationToken),
                "Local" => await CallLocalAIAsync(prompt, cancellationToken),
                _ => throw new NotSupportedException($"AI Provider '{provider}' không được hỗ trợ")
            };
        }

        private async Task<string> CallOpenAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
            
            _logger.LogInformation("🔑 OpenAI Config: Model={Model}, ApiKeyLength={ApiKeyLength}", 
                model, apiKey?.Length ?? 0);
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("❌ OpenAI API Key không được cấu hình hoặc rỗng");
                throw new InvalidOperationException("OpenAI API Key không được cấu hình");
            }

            var request = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1500,
                temperature = 0.7
            };

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("🌐 Gửi request đến OpenAI API...");
            
            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions", 
                content, 
                cancellationToken);
            
            _logger.LogInformation("📡 OpenAI API Response Status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ OpenAI API Error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"OpenAI API Error: {response.StatusCode} - {errorContent}");
            }
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📄 OpenAI Response Content Length: {ContentLength}", responseContent.Length);
            
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
            
            if (result?.choices == null || result.choices.Length == 0)
            {
                _logger.LogError("❌ OpenAI Response không có choices: {ResponseContent}", responseContent);
                throw new InvalidOperationException("OpenAI response không có choices");
            }
            
            var aiContent = result.choices[0].message.content;
            _logger.LogInformation("✅ OpenAI trả về content, độ dài: {ContentLength}", aiContent?.Length ?? 0);
            
            return aiContent;
        }

        private async Task<string> CallClaudeAsync(string prompt, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["AI:Claude:ApiKey"];
            var model = _configuration["AI:Claude:Model"] ?? "claude-3-sonnet-20240229";
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Claude API Key không được cấu hình");
            }

            var request = new
            {
                model = model,
                max_tokens = 1500,
                messages = new[]
                {
                    new { role = "user", content = $"{GetSystemPrompt()}\n\n{prompt}" }
                }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages", 
                content, 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ClaudeResponse>(responseContent);
            
            return result.content[0].text;
        }

        private async Task<string> CallLocalAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var baseUrl = _configuration["AI:Local:BaseUrl"] ?? "http://localhost:11434";
            var model = _configuration["AI:Local:Model"] ?? "llama2";
            
            var request = new
            {
                model = model,
                prompt = $"{GetSystemPrompt()}\n\n{prompt}",
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    max_tokens = 1500
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                $"{baseUrl}/api/generate", 
                content, 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
            
            return result.response;
        }

        private string GetSystemPrompt()
        {
            return @"Bạn là bác sĩ nhi khoa chuyên về tăng trưởng trẻ em với hơn 20 năm kinh nghiệm. 
Bạn có kiến thức sâu về:
- Chuẩn tăng trưởng WHO
- Dinh dưỡng trẻ em
- Phát triển thể chất và tinh thần
- Các bệnh lý ảnh hưởng đến tăng trưởng

Hãy đưa ra khuyến nghị:
- Chính xác, dựa trên khoa học
- Dễ hiểu cho phụ huynh
- Có tính thực tiễn cao
- Luôn nhấn mạnh cần tham vấn bác sĩ khi cần thiết
- Sử dụng emoji và format markdown để dễ đọc
- Viết bằng tiếng Việt";
        }

        private string BuildGrowthPredictionPrompt(GrowthAssessmentContext context)
        {
            var predictionsText = string.Join("\n", context.Predictions.Select(p => 
                $"- **{p.TimeLabel}**: Chiều cao {p.PredictedHeight}cm, Cân nặng {p.PredictedWeight}kg, BMI {p.PredictedBMI:F1}, Vòng đầu {p.PredictedHeadCircumference}cm"));

            return $@"
**THÔNG TIN TRẺ:**
- Tên: {context.Child.FullName}
- Tuổi: {context.Child.AgeInMonths} tháng ({context.Child.AgeInMonths / 12} tuổi {context.Child.AgeInMonths % 12} tháng)
- Giới tính: {context.Child.Gender}
- Ngày sinh: {context.Child.BirthDate:dd/MM/yyyy}

**TÌNH TRẠNG HIỆN TẠI (dựa trên chuẩn WHO):**
- Chiều cao: {context.CurrentAssessment.HeightStatus}
- Cân nặng: {context.CurrentAssessment.WeightStatus}
- BMI: {context.CurrentAssessment.BMIStatus}
- Vòng đầu: {context.CurrentAssessment.HeadCircumferenceStatus}

**XU HƯỚNG TĂNG TRƯỞNG:**
- Xu hướng chiều cao: {context.HeightTrend:F3} (dương = tăng, âm = giảm)
- Xu hướng cân nặng: {context.WeightTrend:F3} (dương = tăng, âm = giảm)

**DỰ ĐOÁN TĂNG TRƯỞNG:**
{predictionsText}

**CHẤT LƯỢNG DỰ ĐOÁN:**
- Độ tin cậy: {context.Quality.ConfidenceLevel} ({context.Quality.OverallConfidence:F1}%)
- Chất lượng dữ liệu: {context.Quality.DataQualityDescription}
- Số điểm dữ liệu: {context.Quality.DataPointsUsed}
- Tính nhất quán xu hướng: {context.Quality.TrendConsistency:F1}%

**CẢNH BÁO:**
{string.Join("\n", context.Quality.QualityWarnings.Select(w => $"- {w}"))}

Hãy phân tích và đưa ra khuyến nghị chi tiết bao gồm:
1. **Đánh giá tổng quan** tình trạng hiện tại
2. **Phân tích xu hướng** tăng trưởng
3. **Khuyến nghị dinh dưỡng** cụ thể
4. **Khuyến nghị vận động** phù hợp độ tuổi
5. **Cảnh báo y tế** nếu có dấu hiệu bất thường
6. **Lịch theo dõi** và tái khám
7. **Lưu ý đặc biệt** về chất lượng dự đoán

Format output bằng markdown với emoji phù hợp.";
        }

        private string BuildBasicAssessmentPrompt(BasicAssessmentContext context)
        {
            var ageWarning = context.IsUsingClosestAge ? 
                $"\n⚠️ **LƯU Ý**: Đánh giá dựa trên độ tuổi chuẩn {context.StandardAgeInMonths} tháng (thay vì {context.RequestedAgeInMonths} tháng yêu cầu)" : "";

            return $@"
**THÔNG TIN TRẺ:**
- Tên: {context.Child.FullName}
- Tuổi: {context.Child.AgeInMonths} tháng ({context.Child.AgeInMonths / 12} tuổi {context.Child.AgeInMonths % 12} tháng)
- Giới tính: {context.Child.Gender}
- Ngày sinh: {context.Child.BirthDate:dd/MM/yyyy}

**CHỈ SỐ HIỆN TẠI:**
- Chiều cao: {context.CurrentRecord.Height}cm
- Cân nặng: {context.CurrentRecord.Weight}kg
- BMI: {context.CurrentRecord.Bmi:F1}
- Vòng đầu: {context.CurrentRecord.HeadCircumference}cm
- Ngày đo: {context.CurrentRecord.CreatedAt:dd/MM/yyyy}

**ĐÁNH GIÁ TÌNH TRẠNG (dựa trên chuẩn WHO):**
- Chiều cao: {context.Assessment.HeightStatus}
- Cân nặng: {context.Assessment.WeightStatus}
- BMI: {context.Assessment.BMIStatus}
- Vòng đầu: {context.Assessment.HeadCircumferenceStatus}
{ageWarning}

Hãy đưa ra khuyến nghị chi tiết bao gồm:
1. **Đánh giá tổng quan** tình trạng hiện tại
2. **Khuyến nghị dinh dưỡng** phù hợp
3. **Khuyến nghị vận động** theo độ tuổi
4. **Cảnh báo y tế** nếu có dấu hiệu bất thường
5. **Lịch theo dõi** và tái khám
6. **Lưu ý đặc biệt** nếu sử dụng độ tuổi gần nhất

Format output bằng markdown với emoji phù hợp.";
        }
    }

    // Response models
    public class OpenAIResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public AIMessage message { get; set; }
    }

    public class AIMessage
    {
        public string content { get; set; }
    }

    public class ClaudeResponse
    {
        public Content[] content { get; set; }
    }

    public class Content
    {
        public string text { get; set; }
    }

    public class OllamaResponse
    {
        public string response { get; set; }
    }
}
