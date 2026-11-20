/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for the login request body that this MVC copy
   of the DTO sends to the RaceDay API.
*/

namespace RaceDay.Web.Dtos
{
    // The credentials sent to POST api/auth/login.
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}