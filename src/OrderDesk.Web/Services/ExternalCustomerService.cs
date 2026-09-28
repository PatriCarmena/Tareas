namespace OrderDesk.Web.Services;

public sealed record CustomerInsight(int CustomerId, int RiskScore, string Segment, string Recommendation, DateTime CheckedAtUtc);

public interface IExternalCustomerService
{
    Task<CustomerInsight?> GetInsightAsync(int customerId, CancellationToken cancellationToken);
}

public sealed class ExternalCustomerService(HttpClient httpClient, ILogger<ExternalCustomerService> logger) : IExternalCustomerService
{
    public async Task<CustomerInsight?> GetInsightAsync(int customerId, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<CustomerInsight>($"/api/customer-insights/{customerId}", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "La API externa no respondió para el cliente {CustomerId}", customerId);
            return null;
        }
    }
}
