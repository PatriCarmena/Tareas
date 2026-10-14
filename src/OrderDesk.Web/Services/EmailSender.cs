using System.Net;
using System.Net.Mail;

namespace OrderDesk.Web.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private string? Host => configuration["Email:SmtpHost"];
    private int Port => configuration.GetValue("Email:SmtpPort", 587);
    private string? Username => configuration["Email:Username"];
    private string? Password => configuration["Email:Password"];
    private string? FromAddress => configuration["Email:FromAddress"] ?? Username;
    private string FromName => configuration["Email:FromName"] ?? "Nexo";
    private bool EnableSsl => configuration.GetValue("Email:EnableSsl", true);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromAddress);

    public async Task SendResetCodeAsync(string email, string code, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("El envío por correo todavía no está configurado.");

        using var message = new MailMessage
        {
            From = new MailAddress(FromAddress!, FromName),
            Subject = "Código para recuperar tu contraseña de Nexo",
            Body = $"Tu código de recuperación de Nexo es: {code}\n\nVence en 15 minutos. Si no solicitaste este cambio, ignorá este mensaje.",
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var smtp = new SmtpClient(Host!, Port)
        {
            EnableSsl = EnableSsl,
            Credentials = new NetworkCredential(Username, Password)
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(message);
        }
        catch (SmtpException exception)
        {
            logger.LogError(exception, "El servidor SMTP rechazó el correo de recuperación para {Email}.", email);
            throw new InvalidOperationException("No se pudo enviar el correo de recuperación.", exception);
        }
    }
}
