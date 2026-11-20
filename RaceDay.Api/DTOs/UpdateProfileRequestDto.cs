
using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: System.ComponentModel.DataAnnotations Namespace
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for validating user profile
       information before updating the database.

       ============================================================ */

    // Stores the information a RaceDay user may update.
    // Email, password and role are excluded for security.
    public class UpdateProfileRequestDto
    {
        /* ========================================================
           SECTION 1: FIRST NAME
           ======================================================== */

        // The user's first name is required.
        [Required(ErrorMessage = "First name is required.")]

        // Limit the name to match the database column.
        [StringLength(60, MinimumLength = 2,
            ErrorMessage =
                "First name must contain between 2 and 60 characters.")]

        public string FirstName { get; set; } = string.Empty;


        /* ========================================================
           SECTION 2: LAST NAME
           ======================================================== */

        // The user's surname is required.
        [Required(ErrorMessage = "Last name is required.")]

        // Keep the surname within the database limit.
        [StringLength(60, MinimumLength = 2,
            ErrorMessage =
                "Last name must contain between 2 and 60 characters.")]

        public string LastName { get; set; } = string.Empty;


        /* ========================================================
           SECTION 3: PHONE NUMBER
           ======================================================== */

        // The user must provide a contact number.
        [Required(ErrorMessage = "Phone number is required.")]

        // Prevent excessively short or long phone numbers.
        [StringLength(20, MinimumLength = 7,
            ErrorMessage =
                "Phone number must contain between 7 and 20 characters.")]

        public string PhoneNumber { get; set; } = string.Empty;


        /* ========================================================
           SECTION 4: DATE OF BIRTH
           ======================================================== */

        // Store only the date without a time component.
        // The controller will reject missing and future dates.
        [Required(ErrorMessage = "Date of birth is required.")]

        public DateOnly DateOfBirth { get; set; }
    }
}
