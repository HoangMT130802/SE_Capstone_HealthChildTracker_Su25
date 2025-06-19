using Microsoft.AspNetCore.Mvc;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new
            {
                message = "Health Child Tracker API is running!",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                swagger = "/swagger",
                endpoints = new
                {
                    authentication = "/api/Authentication",
                    children = "/api/Children",
                    growthRecords = "/api/GrowthRecords",
                    growthStandard = "/api/GrowthStandard"
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
                uptime = Environment.TickCount64
            });
        }

        [HttpGet("swagger")]
        public IActionResult RedirectToSwagger()
        {
            return Redirect("/swagger");
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
                suggestion = "Please check your request and try again, or contact support"
            });
        }
    }
} 