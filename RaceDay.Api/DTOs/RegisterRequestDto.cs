
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: Model Validation in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for implementing input validation
       using DataAnnotations and IValidatableObject.

       ============================================================ */

    // Stores information received when a new user registers.
    // The API will validate this data before creating the account.
    public class RegisterRequestDto : IValidatableObject
    {
        // First name must be provided.
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(60, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        // Last name must be provided.
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(60, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        // Email must follow a valid email address format.
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // Password must meet the minimum length requirement.
        // The original password will never be stored in the database.
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        // Store the user's contact number.
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        // Store the user's date of birth.
        public DateOnly DateOfBirth { get; set; }

        // Allow registration as Organiser or Participant.
        [Required(ErrorMessage = "Role is required.")]
        [RegularExpression(
            "^(Organiser|Participant)$",
            ErrorMessage = "Role must be Organiser or Participant.")]
        public string Role { get; set; } = string.Empty;

        // Validate the date of birth before registration.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Reject missing or future dates of birth.
            if (DateOfBirth == default ||
                DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                yield return new ValidationResult(
                    "Enter a valid date of birth that is not in the future.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}
