
using System;
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

       Usage: Referenced for securely hashing user passwords
       using ASP.NET Core Identity password hashing.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: PasswordVerificationResult Enum
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordverificationresult
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for checking password verification
       results during RaceDay user authentication.

       ============================================================ */

    // Handles password hashing and verification for RaceDay.
    // Original passwords are never stored in the database.
    public class PasswordService : IPasswordService
    {
        // ASP.NET Core Identity provides secure password hashing.
        private readonly IPasswordHasher<User> _passwordHasher;

        // Receive the password hasher through dependency injection.
        public PasswordService(
            IPasswordHasher<User> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        /* ========================================================
           SECTION 1: HASH USER PASSWORD
           ======================================================== */

        // Creates a secure password hash when a user registers.
        public string HashPassword(
            User user,
            string password)
        {
            // Ensure the user object is provided.
            ArgumentNullException.ThrowIfNull(user);

            // Reject missing or empty passwords.
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    "Password cannot be empty.",
                    nameof(password));
            }

            // Hash the password using ASP.NET Core Identity.
            // The hasher generates a random salt automatically.
            string hashedPassword =
                _passwordHasher.HashPassword(user, password);

            // Return the hash to be stored in PASSWORD_HASH.
            return hashedPassword;
        }

        /* ========================================================
           SECTION 2: VERIFY USER PASSWORD
           ======================================================== */

        // Checks the password entered during login.
        public PasswordVerificationResult VerifyPassword(
            User user,
            string hashedPassword,
            string providedPassword)
        {
            // Ensure the user object is provided.
            ArgumentNullException.ThrowIfNull(user);

            // Reject missing password information.
            if (string.IsNullOrWhiteSpace(hashedPassword) ||
                string.IsNullOrWhiteSpace(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            // Compare the entered password with the stored hash.
            PasswordVerificationResult result =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    hashedPassword,
                    providedPassword);

            // Return the verification result to the caller.
            return result;
        }
    }
}
