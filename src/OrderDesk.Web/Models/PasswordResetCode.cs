namespace OrderDesk.Web.Models;

public sealed class PasswordResetCode
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public string CodeHash { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
