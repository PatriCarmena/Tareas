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
        if (user is not null && !string.IsNullOrWhiteSpace(user.PhoneNumber) && whatsApp.IsConfigured)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var entry = new PasswordResetCode { UserId = user.Id, ResetToken = await userManager.GeneratePasswordResetTokenAsync(user), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) };
            entry.CodeHash = new PasswordHasher<PasswordResetCode>().HashPassword(entry, code);
            try
            {
                await whatsApp.SendResetCodeAsync(user.PhoneNumber, code, ct);
                db.PasswordResetCodes.Add(entry);
                await db.SaveChangesAsync(ct);
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = "No pudimos enviar el código por WhatsApp. Verificá que el número asociado esté habilitado como destinatario en Twilio e intentá nuevamente.";
                return RedirectToAction(nameof(ForgotPassword));
            }
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
