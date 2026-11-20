
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

       Usage: Referenced for validating result information
       before saving it to the RaceDay database.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: IValidatableObject Interface
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for applying custom finish-time
       validation to RaceDay result requests.

       ============================================================ */

    // Stores the details required to record a race result.
    public class ResultRequestDto : IValidatableObject
    {
        /* ========================================================
           SECTION 1: ENROLMENT IDENTIFIER
           ======================================================== */

        // A result must belong to an existing enrolment.
        [Range(1, int.MaxValue,
            ErrorMessage = "A valid enrolment ID is required.")]
        public int EnrolmentId { get; set; }


        /* ========================================================
           SECTION 2: FINISH TIME
           ======================================================== */

        // Store the finish time in HH:mm:ss format.
        // TimeOnly matches the SQL TIME(0) column.
        [Required(ErrorMessage = "Finish time is required.")]
        public TimeOnly? FinishTime { get; set; }


        /* ========================================================
           SECTION 3: FINISHING POSITION
           ======================================================== */

        // Finishing positions must be positive numbers.
        [Range(1, int.MaxValue,
            ErrorMessage =
                "Finishing position must be greater than zero.")]
        public int FinishingPosition { get; set; }


        /* ========================================================
           SECTION 4: PUBLICATION STATUS
           ======================================================== */

        // True means the Participant can view the result.
        // False means the result remains unpublished.
        [Required(ErrorMessage = "Publication status is required.")]
        public bool? IsPublished { get; set; }


        /* ========================================================
           SECTION 5: ADDITIONAL VALIDATION
           ======================================================== */

        // Validate finish times beyond the basic required check.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // A finishing duration must be greater than zero.
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
