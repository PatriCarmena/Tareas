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
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                using var response = await httpClient.GetAsync($"/api/customer-insights/{customerId}", cancellationToken);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<CustomerInsight>(cancellationToken: cancellationToken);

                var isColdStart = response.StatusCode is System.Net.HttpStatusCode.BadGateway
                    or System.Net.HttpStatusCode.ServiceUnavailable
                    or System.Net.HttpStatusCode.GatewayTimeout;
                if (!isColdStart || attempt == 10) response.EnsureSuccessStatusCode();

                logger.LogInformation("La API externa se está reactivando. Reintento {Attempt}/10", attempt);
                await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken);
            }
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "La API externa no respondió para el cliente {CustomerId}", customerId);
            return null;
        }
    }
}
