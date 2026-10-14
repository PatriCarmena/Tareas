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

        var authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{AccountSid}:{AuthToken}")));
        var content = await FindTrialContentAsync(authorization, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json");
        request.Headers.Authorization = authorization;
        var form = new Dictionary<string, string>
        {
            ["From"] = $"whatsapp:{From}",
            ["To"] = $"whatsapp:{phoneNumber}"
        };

        var contentSid = content?.Sid ?? ContentSid;
        if (!string.IsNullOrWhiteSpace(contentSid))
        {
            form["ContentSid"] = contentSid;
            var variableNames = content?.Variables ?? ["1", "2"];
            form["ContentVariables"] = JsonSerializer.Serialize(variableNames
                .Select((name, index) => new { name, value = index == 0 ? $"Código Nexo: {code}" : "válido durante 15 minutos" })
                .ToDictionary(x => x.name, x => x.value));
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

    private async Task<TrialContent?> FindTrialContentAsync(AuthenticationHeaderValue authorization, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://content.twilio.com/v1/Content?PageSize=50");
        request.Headers.Authorization = authorization;
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("No se pudieron consultar las plantillas de Twilio. Estado {StatusCode}.", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("contents", out var contents)) return null;

        var available = contents.EnumerateArray()
            .Select(item => new TrialContent(
                item.GetProperty("sid").GetString()!,
                item.TryGetProperty("friendly_name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("variables", out var variables) && variables.ValueKind == JsonValueKind.Object
                    ? variables.EnumerateObject().Select(x => x.Name).ToArray()
                    : []))
            .ToArray();

        logger.LogInformation("Twilio devolvió {Count} plantillas de contenido: {Templates}", available.Length,
            string.Join(", ", available.Select(x => $"{x.Name} ({x.Sid})")));
        return available.FirstOrDefault(x => x.Name.Contains("appointment", StringComparison.OrdinalIgnoreCase))
            ?? available.FirstOrDefault();
    }

    private sealed record TrialContent(string Sid, string Name, string[] Variables);
}
