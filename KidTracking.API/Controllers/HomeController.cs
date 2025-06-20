using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public HomeController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var swaggerUrl = _environment.IsDevelopment() ? "/swagger" : "/";
            
            return Ok(new
            {
                message = "Health Child Tracker API is running!",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                environment = _environment.EnvironmentName,
                swagger = swaggerUrl,
                endpoints = new
                {
                    authentication = "/api/Authentication",
                    children = "/api/Children",
                    growthRecords = "/api/GrowthRecords",
                    growthStandard = "/api/GrowthStandard",
                    growthAssessment = "/api/GrowthAssessment"
                }
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                uptime = Environment.TickCount64,
                environment = _environment.EnvironmentName
            });
        }

        [HttpGet("swagger")]
        public IActionResult RedirectToSwagger()
        {
            if (_environment.IsDevelopment())
            {
                return Redirect("/swagger");
            }
            else
            {
                // Trong production, swagger ở root nên redirect về home
                return Redirect("/");
            }
        }

        [HttpGet("error")]
        [HttpPost("error")]
        [HttpPut("error")]
        [HttpDelete("error")]
        public IActionResult Error()
        {
            return StatusCode(500, new
            {
                error = "An error occurred processing your request",
                timestamp = DateTime.UtcNow,
                suggestion = "Please check your request and try again, or contact support",
                environment = _environment.EnvironmentName
            });
        }
    }
} 