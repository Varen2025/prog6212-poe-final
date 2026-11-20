using System;

namespace RaceDay.Web.Dtos
{
    /* ============================================================
       Code Attribution

       TITLE: Call a web API from a .NET client (HttpClient)
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for mapping the RaceDay API result
       response into an MVC-side data transfer object.

       ============================================================ */

    // Mirrors the API's race result response.
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

        // Number of Completed finishers in the same event.
        public int TotalFinishers { get; set; }

        // Indicates whether the result is published.
        public bool IsPublished { get; set; }

        // Date and time when the result was first published.
        public DateTime? PublishedAt { get; set; }
    }
}