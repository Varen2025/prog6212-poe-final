
using System;
using System.Collections.Generic;

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

       Usage: Referenced for preparing a structured
       dashboard response for an ASP.NET Core API.

       ============================================================ */

    // Represents the full Organiser dashboard response.
    public class OrganiserDashboardDto
    {
        /* ========================================================
           SECTION 1: ORGANISER IDENTIFICATION
           ======================================================== */

        // The logged-in Organiser who owns the dashboard.
        public int OrganiserId { get; set; }


        /* ========================================================
           SECTION 2: OVERALL EVENT STATISTICS
           ======================================================== */

        // Total events created by this Organiser.
        public int TotalEvents { get; set; }

        // Events scheduled for the future.
        public int UpcomingEvents { get; set; }

        // Events with dates already in the past.
        public int PastEvents { get; set; }

        // Total enrolment records across owned events.
        public int TotalEnrolments { get; set; }


        /* ========================================================
           SECTION 3: NEXT UPCOMING EVENT
           ======================================================== */

        // Date of the next upcoming event.
        // Null when the Organiser has no upcoming events.
        public DateTime? NextUpcomingEventDate { get; set; }


        /* ========================================================
           SECTION 4: INDIVIDUAL EVENT SUMMARIES
           ======================================================== */

        // Stores the breakdown for every owned event.
        public List<DashboardEventDto> Events { get; set; }
            = new List<DashboardEventDto>();
    }
}
