using System.ComponentModel.DataAnnotations;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.ViewModels;

public sealed class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Recordarme")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
public sealed class ForgotPasswordVm { [Required, EmailAddress] public string Email { get; set; } = string.Empty; }
public sealed class ResetPasswordVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, StringLength(6, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(8)] public string NewPassword { get; set; } = string.Empty;
    [Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = string.Empty;
}
public sealed class CreateUserVm
{
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Phone] public string? PhoneNumber { get; set; }
    [Required, DataType(DataType.Password), MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = AppRoles.Viewer;
}
public sealed class EditUserVm
{
    [Required] public string Id { get; set; } = string.Empty;
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Phone] public string? PhoneNumber { get; set; }
    [DataType(DataType.Password), MinLength(8)] public string? NewPassword { get; set; }
    [Required] public string Role { get; set; } = AppRoles.Viewer;
}
public sealed record UserListItemVm(string Id, string FullName, string Email, string? Phone, string Role);
