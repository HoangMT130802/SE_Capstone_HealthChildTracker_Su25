using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Repositories.Entities;
using Repositories.Interfaces;
using Repositories.Common;
using Services.Interfaces;
using Services.Implementations;
using Contracts.MapperProfiles;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;
namespace KidTracking.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            builder.Services.AddControllers(options =>
            {
                options.ModelValidatorProviders.Clear();
            })
            .AddJsonOptions(options =>
            {
                // ✅ Thêm TimeOnly converter để xử lý JSON serialization
                options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new TimeOnlyNullableJsonConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                        .ToArray();

                    return new BadRequestObjectResult(new
                    {
                        message = "Dữ liệu không hợp lệ",
                        errors = errors
                    });
                };
            });

            // Cấu hình JWT
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKeyString = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured!");
            }
            
            if (string.IsNullOrEmpty(issuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured!");
            }
            
            if (string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT Audience is not configured!");
            }
            
            var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrEmpty(authHeader))
                        {
                        
                            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Trim();
                            }
                            else
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            }
                            Console.WriteLine($"Token processed: {!string.IsNullOrEmpty(context.Token)}");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("=== JWT Token Validated ===");
                        Console.WriteLine($"User: {context.Principal?.Identity?.Name}");
                        Console.WriteLine($"Claims: {string.Join(", ", context.Principal?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? new string[0])}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("=== JWT Authentication Failed ===");
                        Console.WriteLine($"Exception: {context.Exception?.Message}");
                        Console.WriteLine($"Exception type: {context.Exception?.GetType().Name}");
                        Console.WriteLine($"Stack trace: {context.Exception?.StackTrace}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine("=== JWT Challenge ===");
                        Console.WriteLine($"Error: {context.Error}");
                        Console.WriteLine($"ErrorDescription: {context.ErrorDescription}");
                        context.Response.Headers["Token-Error"] = "Invalid token or no token provided";
                        return Task.CompletedTask;
                    }
                };
            });
            // Swagger với JWT support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Health Child Tracker API", Version = "v1" });
                
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Authorization: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddHttpContextAccessor();
            // singleton của payos
            builder.Services.AddSingleton(new PayOS(
                clientId: builder.Configuration["Environment:PAYOS_CLIENT_ID"],
                apiKey: builder.Configuration["Environment:PAYOS_API_KEY"],
                checksumKey: builder.Configuration["Environment:PAYOS_CHECKSUM_KEY"]));
            // Đăng ký db context
            builder.Services.AddDbContext<HealthChildTrackerContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký các service (chỉ những gì cần cho authentication)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IChildService, ChildService>();
            builder.Services.AddScoped<IGrowthStandardService, GrowthStandardService>();
            builder.Services.AddScoped<IGrowthRecordService, GrowthRecordService>();
            builder.Services.AddScoped<IGrowthAssessmentService, GrowthAssessmentService>();
            builder.Services.AddScoped<IDailyRecordService, DailyRecordService>();
            builder.Services.AddScoped<IDiseaseService, DiseaseService>();
            builder.Services.AddScoped<IVaccineService, VaccineService>();
            builder.Services.AddScoped<IChildVaccineProfileService, ChildVaccineProfileService>();
            builder.Services.AddScoped<IVaccinationFacilityService, VaccinationFacilityService>();
            builder.Services.AddScoped<IVaccinePackageService,VaccinePackageService>();
            builder.Services.AddScoped<IFacilityVaccineService, FacilityVaccineService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            // ✅ Thêm Schedule Slot và Appointment Schedule Services
            builder.Services.AddScoped<IScheduleSlotService, ScheduleSlotService>();
            builder.Services.AddScoped<IAppointmentScheduleService, AppointmentScheduleService>();
            builder.Services.AddScoped<IAppointmentBookingService, AppointmentBookingService>();
            builder.Services.AddScoped<ISurveyService, SurveyService>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<IUserMembershipService, UserMembershipService>();
            // ✅ Thêm Payment và Transaction Services
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();

            builder.Services.AddScoped<IVaccineTemplateService, VaccineTemplateService>();
            // Đăng ký automapper
            builder.Services.AddAutoMapper(typeof(AuthenticationProfile).Assembly);

            // Cấu hình CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            var app = builder.Build();

         
            app.UseExceptionHandler("/error");
            app.UseHsts();

            // Enable Swagger for all environments (including Production)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Health Child Tracker API V1");
                
                
                if (app.Environment.IsDevelopment())
                {
                    c.RoutePrefix = "swagger"; // Local development: /swagger
                }
                else
                {
                    c.RoutePrefix = string.Empty; // Production: root URL
                }
                
                c.DocumentTitle = "Health Child Tracker API";
            });

        
            if (!app.Environment.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
                    await next();
                    Console.WriteLine($"Response: {context.Response.StatusCode}");
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
