/*
   TITLE: Model validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for validating the profile form with
   DataAnnotations and IValidatableObject so the rules match the
   RaceDay API.


   TITLE: DateOnly.TryParseExact Method
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/api/system.dateonly.tryparseexact
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for reading the date of birth typed or picked
   as year-month-day.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace RaceDay.Web.Models
{
    // The data shown and edited on the profile page.
    public class ProfileFormViewModel : IValidatableObject
    {
        // Display only. Nullable so MVC does not treat them as
        // required form fields.
        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? ProfileImageUrl { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Editable fields.
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(60, MinimumLength = 2,
            ErrorMessage =
                "First name must contain between 2 and 60 characters.")]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(60, MinimumLength = 2,
            ErrorMessage =
                "Last name must contain between 2 and 60 characters.")]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20, MinimumLength = 7,
            ErrorMessage =
                "Phone number must contain between 7 and 20 characters.")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Kept as text in the form yyyy-MM-dd from the date input.
        [Required(ErrorMessage = "Date of birth is required.")]
        [Display(Name = "Date of birth")]
        public string DateOfBirth { get; set; } = string.Empty;

        // Reads the date of birth. Returns null when it is not valid.
        public DateOnly? ParseDateOfBirth()
        {
            if (DateOnly.TryParseExact(
                    DateOfBirth.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly parsed))
            {
                return parsed;
            }

            return null;
        }

        // Runs after the field checks pass. Matches the API's rules.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(DateOfBirth))
            {
                yield break;
            }

            DateOnly? parsed = ParseDateOfBirth();

            if (parsed == null ||
                parsed.Value < new DateOnly(1900, 1, 1))
            {
                yield return new ValidationResult(
                    "Enter a valid date of birth.",
                    new[] { nameof(DateOfBirth) });
            }
            else if (parsed.Value >
                DateOnly.FromDateTime(DateTime.Now))
            {
                yield return new ValidationResult(
                    "Date of birth cannot be in the future.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}