namespace MediBookAPI.Services.HealthServices
{
    public record HealthResult(string Status, Dictionary<string, string>? Checks, DateTime Timestamp);

    public interface IHealthService
    {
        HealthResult GetLiveness();
        Task<HealthResult> GetReadinessAsync();
    }
}
