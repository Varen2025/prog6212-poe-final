
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: RegularExpressionAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.regularexpressionattribute
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting enrolment statuses
       to the values allowed by the RaceDay SQL database.

       ============================================================ */

    // Defines the status submitted by an Organiser.
    public class UpdateEnrolmentStatusDto
    {
        // The new enrolment status is required.
        [Required(ErrorMessage = "Enrolment status is required.")]

        // Only accept the four statuses in the SQL CHECK constraint.
        [RegularExpression(
            "^(Pending|Confirmed|Cancelled|Completed)$",
            ErrorMessage =
                "Status must be Pending, Confirmed, Cancelled or Completed.")]

        public string Status { get; set; } = string.Empty;
    }
}
