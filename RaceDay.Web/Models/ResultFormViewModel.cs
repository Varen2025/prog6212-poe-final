/*
   TITLE: Model validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for validating the result form with
   DataAnnotations and IValidatableObject so the finish time and
   position rules match the RaceDay API.


   TITLE: TimeOnly.TryParseExact Method
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/api/system.timeonly.tryparseexact
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for reading the finish time typed as
   hours:minutes:seconds.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace RaceDay.Web.Models
{
    // The data entered on the record and edit result forms.
    public class ResultFormViewModel : IValidatableObject
    {
        public int EventId { get; set; }

        // The enrolment the result belongs to.
        public int EnrolmentId { get; set; }

        // Set when editing. Zero when recording.
        public int ResultId { get; set; }

        // Shown on the page only. Nullable so MVC does not treat
        // them as required form fields.
        public string? EventName { get; set; }

        public string? ParticipantName { get; set; }

        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Finish time is required.")]
        [Display(Name = "Finish time (hh:mm:ss)")]
        public string FinishTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Finishing position is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Finishing position must be 1 or more.")]
        [Display(Name = "Finishing position")]
        public int? FinishingPosition { get; set; }

        [Display(Name = "Publish this result so the Participant can see it")]
        public bool IsPublished { get; set; }

        // Reads the typed time. Returns null when it is not valid.
        public TimeOnly? ParseFinishTime()
        {
            if (TimeOnly.TryParseExact(
                    FinishTime.Trim(),
                    "H:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out TimeOnly parsed))
            {
                return parsed;
            }

            return null;
        }

        // Runs after the field checks pass. Matches the API's rules.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FinishTime))
            {
                yield break;
            }

            TimeOnly? parsed = ParseFinishTime();

            if (parsed == null)
            {
                yield return new ValidationResult(
                    "Enter the time as hours:minutes:seconds, " +
                    "for example 00:45:30.",
                    new[] { nameof(FinishTime) });
            }
            else if (parsed.Value == TimeOnly.MinValue)
            {
                yield return new ValidationResult(
                    "Finish time must be greater than 00:00:00.",
                    new[] { nameof(FinishTime) });
            }
        }
    }
}