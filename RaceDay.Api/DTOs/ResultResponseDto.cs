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

       Usage: Referenced for returning structured race
       result information from ASP.NET Core controllers.

       ============================================================ */

    // Defines the information returned for a race result.
    public class ResultResponseDto
    {
        // Unique result identifier.
        public int ResultId { get; set; }

        // Enrolment associated with the result.
        public int EnrolmentId { get; set; }

        // Participant who completed the event.
        public int ParticipantId { get; set; }

        // Participant's full name.
        public string ParticipantName { get; set; } = string.Empty;

        // Associated event identifier.
        public int EventId { get; set; }

        // Name of the completed event.
        public string EventName { get; set; } = string.Empty;

        // Date of the event.
        public DateTime EventDate { get; set; }

        // Category selected during enrolment.
        public int CategoryId { get; set; }

        // Name of the selected category.
        public string CategoryName { get; set; } = string.Empty;

        // Recorded finishing time.
        public TimeOnly FinishTime { get; set; }

        // Recorded finishing position.
        public int FinishingPosition { get; set; }

        // Indicates whether the result is published.
        public bool IsPublished { get; set; }

        // Date and time when the result was first published.
        public DateTime? PublishedAt { get; set; }

        // Number of Completed enrolments in the same event.
        // Used to show a position such as "47th out of 312 finishers".
        public int TotalFinishers { get; set; }
    }
}