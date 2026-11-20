/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the profile update body that this MVC copy
   of the DTO sends to PUT api/users/me.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The personal details a user may change.
    public class UpdateProfileRequestDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public DateOnly DateOfBirth { get; set; }
    }
}