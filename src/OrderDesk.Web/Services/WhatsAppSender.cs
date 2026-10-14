using System.Net.Http.Headers;
using System.Text.Json;

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
    private string? ContentSid => configuration["Twilio:ContentSid"];
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
        var form = new Dictionary<string, string>
        {
            ["From"] = $"whatsapp:{From}",
            ["To"] = $"whatsapp:{phoneNumber}"
        };

        if (!string.IsNullOrWhiteSpace(ContentSid))
        {
            form["ContentSid"] = ContentSid;
            form["ContentVariables"] = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["1"] = $"Código Nexo: {code}",
                ["2"] = "válido durante 15 minutos"
            });
        }
        else
        {
            form["Body"] = $"Tu código de recuperación de Nexo es {code}. Vence en 15 minutos.";
        }

        request.Content = new FormUrlEncodedContent(form);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Twilio rechazó el mensaje de WhatsApp con estado {StatusCode}: {ResponseBody}",
                (int)response.StatusCode, responseBody);
            throw new InvalidOperationException("Twilio no pudo enviar el código de recuperación.");
        }
    }
}
