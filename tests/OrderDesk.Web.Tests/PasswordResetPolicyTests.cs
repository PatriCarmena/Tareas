using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.Tests;

[TestClass]
public sealed class PasswordResetPolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Resend_IsBlockedDuringFirstMinute_AndAllowedAtBoundary()
    {
        var entry = new PasswordResetCode { ExpiresAtUtc = Now.AddMinutes(15) };
        Assert.IsFalse(PasswordResetPolicy.CanIssue(entry, Now));
        Assert.IsTrue(PasswordResetPolicy.CanIssue(entry, Now.AddMinutes(1)));
    }

    [TestMethod]
    public void FifthWrongCode_InvalidatesTheEntry()
    {
        var entry = new PasswordResetCode { ExpiresAtUtc = Now.AddMinutes(15), FailedAttempts = 4 };
        PasswordResetPolicy.RecordFailure(entry, Now);
        Assert.AreEqual(5, entry.FailedAttempts);
        Assert.AreEqual(Now, entry.UsedAtUtc);
        Assert.IsFalse(PasswordResetPolicy.CanAttempt(entry, Now));
    }

    [TestMethod]
    public void ExpiredCode_IsRejected()
    {
        var entry = new PasswordResetCode { ExpiresAtUtc = Now };
        Assert.IsFalse(PasswordResetPolicy.CanAttempt(entry, Now));
    }
}
