using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace OrderDesk.Web.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken);
    Task SendTransactionalAsync(string email, string subject, string body, CancellationToken cancellationToken);
}

public sealed class GmailApiEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GmailApiEmailSender> logger) : IEmailSender
{
    private string? ClientId => configuration["GmailApi:ClientId"];
    private string? ClientSecret => configuration["GmailApi:ClientSecret"];
    private string? RefreshToken => configuration["GmailApi:RefreshToken"];
    private string? FromAddress => configuration["GmailApi:FromAddress"];
    private string FromName => configuration["GmailApi:FromName"] ?? "Nexo";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RefreshToken)
        && !string.IsNullOrWhiteSpace(FromAddress);

    public async Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken)
        => await SendTransactionalAsync(email, "Código para recuperar tu contraseña de Nexo",
            $"Tu código de recuperación de Nexo es: {code}\n\nVence en 15 minutos. Si no solicitaste este cambio, ignorá este mensaje.", cancellationToken);

    public async Task SendTransactionalAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("El envío por Gmail todavía no está configurado.");
        if (email.Contains('\r') || email.Contains('\n') || subject.Contains('\r') || subject.Contains('\n'))
            throw new ArgumentException("El destinatario o asunto no es válido.");

        try
        {
            using var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ClientId!,
                    ["client_secret"] = ClientSecret!,
                    ["refresh_token"] = RefreshToken!,
                    ["grant_type"] = "refresh_token"
                }),
                cancellationToken);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                logger.LogError("Google rechazó la renovación de autorización. Estado: {StatusCode}.", (int)tokenResponse.StatusCode);
                throw new InvalidOperationException("No se pudo autorizar el envío por Gmail.");
            }

            var token = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new InvalidOperationException("Google no devolvió una autorización válida.");

            var message = string.Join("\r\n",
                $"From: {EncodeHeader(FromName)} <{FromAddress}>",
                $"To: {email}",
                $"Subject: {EncodeHeader(subject)}",
                "MIME-Version: 1.0",
                "Content-Type: text/plain; charset=UTF-8",
                "Content-Transfer-Encoding: base64",
                "",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(body)));

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            request.Content = JsonContent.Create(new { raw = ToBase64Url(message) });
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Gmail API rechazó un correo transaccional para {Email}. Estado: {StatusCode}. Respuesta: {ResponseBody}",
                email,
                (int)response.StatusCode,
                responseBody);
            throw new InvalidOperationException("No se pudo enviar el correo transaccional.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Gmail API no respondió a tiempo al enviar un correo transaccional para {Email}.", email);
            throw new InvalidOperationException("El servicio de correo no respondió a tiempo.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No se pudo conectar con Gmail API para enviar un correo transaccional a {Email}.", email);
            throw new InvalidOperationException("No se pudo conectar con el servicio de correo.", exception);
        }
    }

    private static string EncodeHeader(string value) => $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";

    private static string ToBase64Url(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }
}
