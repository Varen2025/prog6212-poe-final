
using System;
using System.Collections.Generic;
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

       Usage: Referenced for validating event information
       before saving it to the RaceDay database.

       ============================================================ */

    // Contains the information needed to create or update
    // a running, walking or cycling event.
    public class EventRequestDto : IValidatableObject
    {
        /* ========================================================
           SECTION 1: EVENT NAME
           ======================================================== */

        // Every event must have a name.
        [Required(ErrorMessage = "Event name is required.")]

        // Match the NAME VARCHAR(150) database column.
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;


        /* ========================================================
           SECTION 2: EVENT DESCRIPTION
           ======================================================== */

        // Every event must have a description.
        [Required(ErrorMessage = "Description is required.")]

        // Match the DESCRIPTION VARCHAR(1000) column.
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;


        /* ========================================================
           SECTION 3: EVENT DATE
           ======================================================== */

        // Date and starting time of the event.
        [Required(ErrorMessage = "Event date is required.")]
        public DateTime EventDate { get; set; }


        /* ========================================================
           SECTION 4: EVENT LOCATION
           ======================================================== */

        // Location where the event will take place.
        [Required(ErrorMessage = "Location is required.")]

        // Match the LOCATION VARCHAR(200) column.
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;


        /* ========================================================
           SECTION 5: EVENT DISTANCE
           ======================================================== */

        // Distance measured in kilometres.
        // The decimal range matches DECIMAL(6,2).
        [Range(
            typeof(decimal),
            "0.01",
            "9999.99",
            ErrorMessage =
                "Distance must be between 0.01 and 9999.99 km.")]
        public decimal DistanceKm { get; set; }


        /* ========================================================
           SECTION 6: EVENT TYPE
           ======================================================== */

        // Event type must be Run, Walk or Cycle.
        [Required(ErrorMessage = "Event type is required.")]

        // Match the EVENT_TYPE database CHECK constraint.
        [RegularExpression(
            "^(Run|Walk|Cycle)$",
            ErrorMessage =
                "Event type must be Run, Walk or Cycle.")]
        public string EventType { get; set; } = string.Empty;


        /* ========================================================
           SECTION 7: ADDITIONAL VALIDATION
           ======================================================== */

        // Validate values that cannot be checked fully
        // using standard property attributes alone.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Reject a date that was not provided.
            if (EventDate == DateTime.MinValue)
            {
                yield return new ValidationResult(
                    "A valid event date is required.",
                    new[] { nameof(EventDate) });
            }

            // Reject names containing only spaces.
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Event name cannot be empty.",
                    new[] { nameof(Name) });
            }

            // Reject descriptions containing only spaces.
            if (string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult(
                    "Description cannot be empty.",
                    new[] { nameof(Description) });
            }

            // Reject locations containing only spaces.
            if (string.IsNullOrWhiteSpace(Location))
            {
                yield return new ValidationResult(
                    "Location cannot be empty.",
                    new[] { nameof(Location) });
            }
        }
    }
}
