
using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services
{
    /* ============================================================
       Code Attribution

       TITLE: PasswordHasher<TUser> Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for defining secure password hashing
       and password verification operations in RaceDay.

       ============================================================ */

    // Defines the password operations required by the RaceDay API.
    // The implementation will use ASP.NET Core Identity.
    public interface IPasswordService
    {
        // Creates a secure hash from a user's original password.
        string HashPassword(User user, string password);

        // Checks an entered password against its stored hash.
        // The result indicates failure, success or rehashing needed.
        PasswordVerificationResult VerifyPassword(
            User user,
            string hashedPassword,
            string providedPassword);
    }
}
