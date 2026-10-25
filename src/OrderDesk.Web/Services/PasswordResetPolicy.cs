using OrderDesk.Web.Models;

namespace OrderDesk.Web.Services;

public static class PasswordResetPolicy
{
    public const int MaxFailedAttempts = 5;

    public static bool CanIssue(PasswordResetCode? latest, DateTime nowUtc) =>
        latest is null || latest.ExpiresAtUtc <= nowUtc.AddMinutes(14);

    public static bool CanAttempt(PasswordResetCode? entry, DateTime nowUtc) =>
        entry is not null && entry.UsedAtUtc is null && entry.ExpiresAtUtc > nowUtc && entry.FailedAttempts < MaxFailedAttempts;

    public static void RecordFailure(PasswordResetCode entry, DateTime nowUtc)
    {
        entry.FailedAttempts++;
        if (entry.FailedAttempts >= MaxFailedAttempts) entry.UsedAtUtc = nowUtc;
    }
}
