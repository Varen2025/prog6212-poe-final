
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

       Usage: Referenced for returning structured event
       information through the RaceDay dashboard API.

       ============================================================ */

    // Stores the dashboard summary for an individual event.
    public class DashboardEventDto
    {
        /* ========================================================
           SECTION 1: EVENT INFORMATION
           ======================================================== */

        // Unique identifier of the event.
        public int EventId { get; set; }

        // Name of the Organiser's event.
        public string Name { get; set; } = string.Empty;

        // Date and starting time of the event.
        public DateTime EventDate { get; set; }

        // Location where the event takes place.
        public string Location { get; set; } = string.Empty;

        // Run, Walk or Cycle.
        public string EventType { get; set; } = string.Empty;

        // Indicates whether the event is upcoming.
        public bool IsUpcoming { get; set; }


        /* ========================================================
           SECTION 2: ENROLMENT SUMMARY
           ======================================================== */

        // Total number of enrolment records for this event.
        public int TotalEnrolments { get; set; }

        // Enrolments waiting for confirmation.
        public int PendingEnrolments { get; set; }

        // Enrolments confirmed by the Organiser.
        public int ConfirmedEnrolments { get; set; }

        // Enrolments marked as completed.
        public int CompletedEnrolments { get; set; }

        // Enrolments cancelled by the system or Organiser.
        public int CancelledEnrolments { get; set; }
    }
}
