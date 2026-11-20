
using System;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: Create Web APIs with ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/web-api/
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for returning structured response
       objects from ASP.NET Core Web API controllers.

       ============================================================ */

    // Represents the information returned after registration.
    // Passwords and password hashes are never included.
    public class RegisterResponseDto
    {
        // The database-generated identifier of the new user.
        public int UserId { get; set; }

        // The user's registered first name.
        public string FirstName { get; set; } = string.Empty;

        // The user's registered surname.
        public string LastName { get; set; } = string.Empty;

        // The email address associated with the account.
        public string Email { get; set; } = string.Empty;

        // The assigned Organiser or Participant role.
        public string Role { get; set; } = string.Empty;

        // The date and time the account was created.
        public DateTime CreatedAt { get; set; }

        // A message confirming successful registration.
        public string Message { get; set; } = string.Empty;
    }
}
