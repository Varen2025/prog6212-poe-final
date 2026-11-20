/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for the registration request body that this
   MVC copy of the DTO sends to the RaceDay API.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The details sent to POST api/auth/register.
    public class RegisterRequestDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}