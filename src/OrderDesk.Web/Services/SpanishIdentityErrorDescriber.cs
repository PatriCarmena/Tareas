using Microsoft.AspNetCore.Identity;

namespace OrderDesk.Web.Services;

public sealed class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string? email) => Error(nameof(DuplicateEmail), $"El email {email} ya está registrado.");
    public override IdentityError DuplicateUserName(string? userName) => Error(nameof(DuplicateUserName), $"El usuario {userName} ya está registrado.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), $"El email {email} no es válido.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), $"El nombre de usuario {userName} no es válido.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una letra minúscula.");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una letra mayúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir al menos un símbolo.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "La contraseña actual es incorrecta.");
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "No se pudo completar la operación.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
