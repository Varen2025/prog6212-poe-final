/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for the registration response returned by the
   RaceDay API.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The details returned by POST api/auth/register.
    public class RegisterResponseDto
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}