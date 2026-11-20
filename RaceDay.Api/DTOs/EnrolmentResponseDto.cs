
using System;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: Create Web APIs with ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/web-api/
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for returning structured enrolment
       responses from the RaceDay API.

       ============================================================ */

    // Defines the information returned for an enrolment.
    public class EnrolmentResponseDto
    {
        // Unique identifier of the enrolment.
        public int EnrolmentId { get; set; }

        // ID of the Participant who entered.
        public int ParticipantId { get; set; }

        // Participant's name.
        public string ParticipantName { get; set; } = string.Empty;

        // Event that the Participant entered.
        public int EventId { get; set; }

        // Name of the selected event.
        public string EventName { get; set; } = string.Empty;

        // Scheduled date and time of the event.
        public DateTime EventDate { get; set; }

        // Selected category identifier.
        public int CategoryId { get; set; }

        // Selected category name.
        public string CategoryName { get; set; } = string.Empty;

        // Date and time the Participant enrolled.
        public DateTime EnrolmentDate { get; set; }

        // Pending, Confirmed, Cancelled or Completed.
        public string Status { get; set; } = string.Empty;
    }
}
