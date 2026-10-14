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

    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? AppRoles.Viewer;
        return View(new EditUserVm { Id = user.Id, FullName = user.FullName, Email = user.Email ?? string.Empty, PhoneNumber = user.PhoneNumber, Role = role });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserVm model)
    {
        if (!AppRoles.All.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Rol inválido.");
        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();
        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(AppRoles.Administrator) && model.Role != AppRoles.Administrator &&
            await userManager.GetUsersInRoleAsync(AppRoles.Administrator) is { Count: 1 })
            ModelState.AddModelError(nameof(model.Role), "No se puede quitar el rol al último administrador.");
        if (!ModelState.IsValid) return View(model);

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = userManager.NormalizeName(user.UserName);
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) { foreach (var error in update.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded) { foreach (var error in passwordResult.Errors) ModelState.AddModelError(nameof(model.NewPassword), error.Description); return View(model); }
        }

        if (!currentRoles.Contains(model.Role))
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, model.Role);
        }
        TempData["Success"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (user.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "No podés eliminar el usuario con el que tenés la sesión iniciada.";
            return RedirectToAction(nameof(Index));
        }
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.Administrator) && await userManager.GetUsersInRoleAsync(AppRoles.Administrator) is { Count: 1 })
        {
            TempData["Error"] = "No se puede eliminar el último administrador.";
            return RedirectToAction(nameof(Index));
        }
        var result = await userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Usuario eliminado correctamente." : "No se pudo eliminar el usuario.";
        return RedirectToAction(nameof(Index));
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
