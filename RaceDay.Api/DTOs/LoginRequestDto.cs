
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: RequiredAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for validating required fields
       in the RaceDay login request.

       ============================================================ */

    // Represents the credentials submitted during login.
    public class LoginRequestDto
    {
        // The email address registered to the account.
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        // The original password entered by the user.
        // It will be checked against the stored password hash.
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}
