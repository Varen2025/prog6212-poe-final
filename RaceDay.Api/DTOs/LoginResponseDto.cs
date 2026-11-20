
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

       Usage: Referenced for returning structured login
       responses from ASP.NET Core API controllers.

       ============================================================ */

    // Stores the details returned after a successful login.
    // The password and password hash are never included.
    public class LoginResponseDto
    {
        // Unique identifier of the logged-in user.
        public int UserId { get; set; }

        // First name of the logged-in user.
        public string FirstName { get; set; } = string.Empty;

        // Surname of the logged-in user.
        public string LastName { get; set; } = string.Empty;

        // Registered email address.
        public string Email { get; set; } = string.Empty;

        // Identifies the user as Organiser or Participant.
        public string Role { get; set; } = string.Empty;

        // Message confirming that login was successful.
        public string Message { get; set; } = string.Empty;
    }
}
