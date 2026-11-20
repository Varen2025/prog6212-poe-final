
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

       Usage: Referenced for validating event and category
       identifiers supplied during Participant enrolment.

       ============================================================ */

    // Stores the information submitted when a Participant
    // selects an event and category to enter.
    public class EnrolmentRequestDto
    {
        /* ========================================================
           SECTION 1: EVENT IDENTIFIER
           ======================================================== */

        // An event ID is required and must be greater than zero.
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "A valid event ID is required.")]
        public int EventId { get; set; }


        /* ========================================================
           SECTION 2: CATEGORY IDENTIFIER
           ======================================================== */

        // The selected category ID must also be valid.
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "A valid category ID is required.")]
        public int CategoryId { get; set; }
    }
}
