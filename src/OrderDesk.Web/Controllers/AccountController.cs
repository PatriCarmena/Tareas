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
    AppDbContext db, IEmailSender emailSender, IConfiguration configuration) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVm { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
        TempData["Error"] = result.IsLockedOut
            ? "La cuenta está temporalmente bloqueada por varios intentos fallidos. Intentá nuevamente más tarde."
            : "No se pudo iniciar sesión. Revisá el email y la contraseña e intentá nuevamente.";
        return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signInManager.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    [AllowAnonymous]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(configuration["Google:ClientId"])) { TempData["Error"] = "El acceso con Google aún no está configurado."; return RedirectToAction(nameof(Login)); }
        var redirect = Url.Action(nameof(GoogleCallback), new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirect);
        return Challenge(properties, "Google");
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
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Google no proporcionó un correo válido.";
                return RedirectToAction(nameof(Login));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                TempData["Error"] = "No existe un usuario de Nexo registrado con esa cuenta de Google.";
                return RedirectToAction(nameof(Login));
            }

            var loginAdded = await userManager.AddLoginAsync(user, info);
            if (!loginAdded.Succeeded)
            {
                TempData["Error"] = "No se pudo vincular la cuenta de Google con el usuario de Nexo.";
                return RedirectToAction(nameof(Login));
            }

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
        if (user is not null && !string.IsNullOrWhiteSpace(user.Email) && emailSender.IsConfigured)
        {
            var now = DateTime.UtcNow;
            var latest = await db.PasswordResetCodes.Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            // Como máximo un envío por minuto para cada cuenta.
            if (PasswordResetPolicy.CanIssue(latest, now))
            {
                var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
                var entry = new PasswordResetCode { UserId = user.Id, ExpiresAtUtc = now.AddMinutes(15) };
                entry.CodeHash = new PasswordHasher<PasswordResetCode>().HashPassword(entry, code);
                try
                {
                    await emailSender.SendResetCodeAsync(user.Email, code, ct);
                    db.PasswordResetCodes.Add(entry);
                    await db.SaveChangesAsync(ct);
                }
                catch (InvalidOperationException)
                {
                    // La respuesta no revela si la dirección corresponde a una cuenta.
                }
            }
        }
        TempData["Success"] = "Si la cuenta existe, enviamos un código por correo electrónico válido durante 15 minutos.";
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [AllowAnonymous] public IActionResult ResetPassword(string? email = null) => View(new ResetPasswordVm { Email = email ?? string.Empty });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        var now = DateTime.UtcNow;
        var entry = user is null ? null : await db.PasswordResetCodes.Where(x => x.UserId == user.Id && x.UsedAtUtc == null && x.ExpiresAtUtc > now).OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (!PasswordResetPolicy.CanAttempt(entry, now))
        { ModelState.AddModelError(nameof(model.Code), "El código es inválido o venció."); return View(model); }
        if (new PasswordHasher<PasswordResetCode>().VerifyHashedPassword(entry!, entry.CodeHash, model.Code) == PasswordVerificationResult.Failed)
        {
            PasswordResetPolicy.RecordFailure(entry, now);
            await db.SaveChangesAsync(ct);
            ModelState.AddModelError(nameof(model.Code), "El código es inválido o venció.");
            return View(model);
        }
        // El token se genera al confirmar el código, para que un reinicio del servidor
        // no invalide un código que sigue vigente en PostgreSQL.
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        var result = await userManager.ResetPasswordAsync(user!, token, model.NewPassword);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        await db.PasswordResetCodes.Where(x => x.UserId == user!.Id && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow), ct);
        TempData["Success"] = "Contraseña actualizada. Ya podés iniciar sesión."; return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccessDenied() => View();
}
