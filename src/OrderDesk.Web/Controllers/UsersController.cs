using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrderDesk.Web.Models;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public sealed class UsersController(UserManager<AppUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = new List<UserListItemVm>();
        var users = userManager.Users.OrderBy(x => x.FullName).ToList();
        foreach (var user in users)
            result.Add(new(user.Id, user.FullName, user.Email ?? "", user.PhoneNumber, (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? AppRoles.Viewer));
        return View(result);
    }
    public IActionResult Create() => View(new CreateUserVm());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserVm model)
    {
        if (!AppRoles.All.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Rol inválido.");
        if (!ModelState.IsValid) return View(model);
        var user = new AppUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, PhoneNumber = model.PhoneNumber, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        await userManager.AddToRoleAsync(user, model.Role); TempData["Success"] = "Usuario creado correctamente."; return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        if (!AppRoles.All.Contains(role)) return BadRequest();
        var user = await userManager.FindByIdAsync(id); if (user is null) return NotFound();
        var current = await userManager.GetRolesAsync(user); await userManager.RemoveFromRolesAsync(user, current); await userManager.AddToRoleAsync(user, role);
        TempData["Success"] = "Rol actualizado."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePhone(string id, string? phoneNumber)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim().Replace(" ", "").Replace("-", "");

        if (normalizedPhone is not null &&
            (!normalizedPhone.StartsWith('+') || normalizedPhone.Skip(1).Any(character => !char.IsDigit(character))))
        {
            TempData["Error"] = "El WhatsApp debe usar formato internacional, por ejemplo +5493811234567.";
            return RedirectToAction(nameof(Index));
        }

        var result = await userManager.SetPhoneNumberAsync(user, normalizedPhone);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "WhatsApp actualizado correctamente."
            : "No se pudo actualizar el WhatsApp.";
        return RedirectToAction(nameof(Index));
    }
}
