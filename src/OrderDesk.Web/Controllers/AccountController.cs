using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;
using System.Security.Cryptography;

namespace OrderDesk.Web.Controllers;

public sealed class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager,
    AppDbContext db, IWhatsAppSender whatsApp, IConfiguration configuration) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
        return View(model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    [AllowAnonymous]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(configuration["Google:ClientId"])) { TempData["Error"] = "El acceso con Google aún no está configurado."; return RedirectToAction(nameof(Login)); }
        var redirect = Url.Action(nameof(GoogleCallback), new { returnUrl });
        return Challenge(new AuthenticationProperties { RedirectUri = redirect }, "Google");
    }

    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null) { TempData["Error"] = "No se pudo validar la cuenta de Google."; return RedirectToAction(nameof(Login)); }
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
        if (!result.Succeeded)
        {
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email)) return RedirectToAction(nameof(Login));
            var user = await userManager.FindByEmailAsync(email);
            if (user is null) { user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = info.Principal.Identity?.Name ?? email }; var created = await userManager.CreateAsync(user); if (!created.Succeeded) return RedirectToAction(nameof(Login)); }
            await userManager.AddLoginAsync(user, info);
            if (!(await userManager.GetRolesAsync(user)).Any()) await userManager.AddToRoleAsync(user, AppRoles.Viewer);
            await signInManager.SignInAsync(user, false);
        }
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Dashboard")!);
    }

    [AllowAnonymous] public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is not null && !string.IsNullOrWhiteSpace(user.PhoneNumber) && whatsApp.IsConfigured)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var entry = new PasswordResetCode { UserId = user.Id, ResetToken = await userManager.GeneratePasswordResetTokenAsync(user), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) };
            entry.CodeHash = new PasswordHasher<PasswordResetCode>().HashPassword(entry, code);
            db.PasswordResetCodes.Add(entry); await db.SaveChangesAsync(ct);
            await whatsApp.SendResetCodeAsync(user.PhoneNumber, code, ct);
        }
        TempData["Success"] = "Si la cuenta existe y tiene WhatsApp configurado, enviamos un código válido por 15 minutos.";
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [AllowAnonymous] public IActionResult ResetPassword(string? email = null) => View(new ResetPasswordVm { Email = email ?? string.Empty });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        var entry = user is null ? null : await db.PasswordResetCodes.Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (entry is null || new PasswordHasher<PasswordResetCode>().VerifyHashedPassword(entry, entry.CodeHash, model.Code) == PasswordVerificationResult.Failed)
        { ModelState.AddModelError(nameof(model.Code), "El código es inválido o venció."); return View(model); }
        var result = await userManager.ResetPasswordAsync(user!, entry.ResetToken, model.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        entry.UsedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct);
        TempData["Success"] = "Contraseña actualizada. Ya podés iniciar sesión."; return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccessDenied() => View();
}
