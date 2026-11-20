
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: IValidatableObject Interface
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for validating corrected
       finishing times and positions.

       ============================================================ */

    // Stores allowed changes to an existing race result.
    public class UpdateResultRequestDto : IValidatableObject
    {
        // The corrected finishing time is required.
        [Required(ErrorMessage = "Finish time is required.")]
        public TimeOnly? FinishTime { get; set; }

        // Finishing position cannot be zero or negative.
        [Range(1, int.MaxValue,
            ErrorMessage =
                "Finishing position must be greater than zero.")]
        public int FinishingPosition { get; set; }

        // The Organiser must specify whether to publish.
        [Required(ErrorMessage = "Publication status is required.")]
        public bool? IsPublished { get; set; }

        // Apply the same time validation used during creation.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (FinishTime.HasValue &&
                FinishTime.Value == TimeOnly.MinValue)
            {
                yield return new ValidationResult(
                    "Finish time must be greater than 00:00:00.",
                    new[] { nameof(FinishTime) });
            }
        }
    }
}
