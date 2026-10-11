using System.Net.Http.Headers;

namespace OrderDesk.Web.Services;

public interface IWhatsAppSender
{
    bool IsConfigured { get; }
    Task SendResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}

public sealed class TwilioWhatsAppSender(HttpClient http, IConfiguration configuration, ILogger<TwilioWhatsAppSender> logger) : IWhatsAppSender
{
    private string? AccountSid => configuration["Twilio:AccountSid"];
    private string? AuthToken => configuration["Twilio:AuthToken"];
    private string? From => configuration["Twilio:WhatsAppFrom"];
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken) && !string.IsNullOrWhiteSpace(From);

    public async Task SendResetCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("WhatsApp no está configurado. Código de recuperación para {Phone}: {Code}", phoneNumber, code);
            throw new InvalidOperationException("El envío por WhatsApp todavía no está configurado.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{AccountSid}:{AuthToken}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = $"whatsapp:{From}", ["To"] = $"whatsapp:{phoneNumber}", ["Body"] = $"Tu código de recuperación de Nexo es {code}. Vence en 15 minutos."
        });
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
