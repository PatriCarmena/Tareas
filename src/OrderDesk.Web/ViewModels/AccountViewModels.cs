using System.ComponentModel.DataAnnotations;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.ViewModels;

public sealed class LoginVm
{
    [Required(ErrorMessage = "Ingresá tu email."), EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá tu contraseña."), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Recordarme")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
public sealed class ForgotPasswordVm { [Required(ErrorMessage = "Ingresá tu email."), EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = string.Empty; }
public sealed class ResetPasswordVm
{
    [Required(ErrorMessage = "Ingresá tu email."), EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el código."), StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")] public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá una contraseña nueva."), DataType(DataType.Password), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")] public string NewPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "Repetí la contraseña nueva."), Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden."), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
}
public sealed class CreateUserVm
{
    [Required(ErrorMessage = "Ingresá el nombre completo."), StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el email."), EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Ingresá un teléfono válido.")] public string? PhoneNumber { get; set; }
    [Required(ErrorMessage = "Ingresá una contraseña temporal."), DataType(DataType.Password), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")] public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Seleccioná un rol.")] public string Role { get; set; } = AppRoles.Viewer;
}
public sealed class EditUserVm
{
    [Required] public string Id { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el nombre completo."), StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el email."), EmailAddress(ErrorMessage = "Ingresá un email válido.")] public string Email { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Ingresá un teléfono válido.")] public string? PhoneNumber { get; set; }
    [DataType(DataType.Password), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")] public string? NewPassword { get; set; }
    [Required(ErrorMessage = "Seleccioná un rol.")] public string Role { get; set; } = AppRoles.Viewer;
}
public sealed record UserListItemVm(string Id, string FullName, string Email, string? Phone, string Role);
