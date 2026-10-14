using System.Net.Http.Json;

namespace OrderDesk.Web.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken);
}

public sealed class BrevoEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BrevoEmailSender> logger) : IEmailSender
{
    private string? ApiKey => configuration["Brevo:ApiKey"];
    private string? FromAddress => configuration["Brevo:FromAddress"];
    private string FromName => configuration["Brevo:FromName"] ?? "Nexo";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(FromAddress);

    public async Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("El envío por correo todavía no está configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
        request.Headers.Add("api-key", ApiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { name = FromName, email = FromAddress },
            to = new[] { new { email } },
            subject = "Código para recuperar tu contraseña de Nexo",
            textContent = $"Tu código de recuperación de Nexo es: {code}\n\nVence en 15 minutos. Si no solicitaste este cambio, ignorá este mensaje."
        });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Brevo rechazó el correo de recuperación para {Email}. Estado: {StatusCode}. Respuesta: {ResponseBody}",
                email,
                (int)response.StatusCode,
                responseBody);
            throw new InvalidOperationException("No se pudo enviar el correo de recuperación.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Brevo no respondió a tiempo al enviar el correo de recuperación para {Email}.", email);
            throw new InvalidOperationException("El servicio de correo no respondió a tiempo.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No se pudo conectar con Brevo para enviar el correo de recuperación a {Email}.", email);
            throw new InvalidOperationException("No se pudo conectar con el servicio de correo.", exception);
        }
    }
}
