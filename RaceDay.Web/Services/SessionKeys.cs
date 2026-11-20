/*
   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for storing the logged-in user's details
   in server-side session state using named keys.
*/

namespace RaceDay.Web.Services
{
    // Central list of the keys stored in the MVC session.
    // Using constants avoids typing mistakes in magic strings.
    public static class SessionKeys
    {
        // The logged-in user's ID.
        public const string UserId = "UserId";

        // The logged-in user's role (Organiser or Participant).
        public const string Role = "Role";

        // The logged-in user's first name, shown in the navigation bar.
        public const string FirstName = "FirstName";

        // The API's own session cookie value for this user.
        public const string ApiSessionCookie = "ApiSessionCookie";
    }
}