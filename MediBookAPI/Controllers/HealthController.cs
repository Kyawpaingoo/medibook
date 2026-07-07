using MediBookAPI.Services.HealthServices;
using Microsoft.AspNetCore.Mvc;

namespace MediBookAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet]
        public IActionResult Liveness()
        {
            var result = _healthService.GetLiveness();
            return Ok(result);
        }

        [HttpGet("ready")]
        public async Task<IActionResult> Readiness()
        {
            var result = await _healthService.GetReadinessAsync();

            return result.Status == "Healthy"
                ? Ok(result)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
    }
}
