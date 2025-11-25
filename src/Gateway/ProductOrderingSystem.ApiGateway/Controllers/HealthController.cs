using Microsoft.AspNetCore.Mvc;

namespace ProductOrderingSystem.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                service = "API Gateway",
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                routes = new[]
                {
                    new { path = "/api/products", service = "Product Service", port = "7001" },
                    new { path = "/api/orders", service = "Order Service", port = "7002" },
                    new { path = "/api/users", service = "User Service", port = "7003" }
                }
            });
        }

        [HttpGet("services")]
        public IActionResult GetServices()
        {
            return Ok(new
            {
                services = new[]
                {
                    new { 
                        name = "Product Service", 
                        url = "https://localhost:7001",
                        healthCheck = "https://localhost:7001/api/health",
                        documentation = "https://localhost:7001/scalar/v1"
                    },
                    new { 
                        name = "Order Service", 
                        url = "https://localhost:7002",
                        healthCheck = "https://localhost:7002/api/health",
                        documentation = "https://localhost:7002/scalar/v1"
                    },
                    new { 
                        name = "User Service", 
                        url = "https://localhost:7003",
                        healthCheck = "https://localhost:7003/api/health",
                        documentation = "https://localhost:7003/scalar/v1"
                    }
                }
            });
        }
    }
}