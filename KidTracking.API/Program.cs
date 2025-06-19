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

namespace KidTracking.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Cấu hình JWT
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKeyString = jwtSettings["SecretKey"];
            
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured!");
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
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
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
                            // Nếu header không bắt đầu bằng "Bearer ", thêm vào
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
                        context.Response.Headers.Add("Token-Error", "Invalid token or no token provided");
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

            // Add HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Thêm CORS middleware
            app.UseCors("AllowAll");

            // Thêm Authentication middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
