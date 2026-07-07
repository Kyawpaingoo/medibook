using Data.Models;
using StackExchange.Redis;

namespace MediBookAPI.Services.HealthServices
{
    public class HealthService : IHealthService
    {
        private readonly BookingDBContext _db;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<HealthService> _logger;

        public HealthService(BookingDBContext db, IConnectionMultiplexer redis, ILogger<HealthService> logger)
        {
            _db = db;
            _redis = redis;
            _logger = logger;
        }

        public HealthResult GetLiveness()
        {
            return new HealthResult("Healthy", null, DateTime.UtcNow);
        }

        public async Task<HealthResult> GetReadinessAsync()
        {
            var checks = new Dictionary<string, string>();

            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                checks["postgresql"] = canConnect ? "Healthy" : "Unhealthy";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL readiness check failed");
                checks["postgresql"] = $"Unhealthy: {ex.Message}";
            }

            try
            {
                await _redis.GetDatabase().PingAsync();
                checks["redis"] = _redis.IsConnected ? "Healthy" : "Unhealthy";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis readiness check failed");
                checks["redis"] = $"Unhealthy: {ex.Message}";
            }

            var allHealthy = checks.Values.All(v => v == "Healthy");

            return new HealthResult(
                allHealthy ? "Healthy" : "Unhealthy",
                checks,
                DateTime.UtcNow
            );
        }
    }
}
