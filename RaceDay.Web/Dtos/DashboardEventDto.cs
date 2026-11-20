/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for the per-event dashboard data returned by the
   RaceDay API, which this MVC copy of the DTO mirrors.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The summary of one event on the Organiser dashboard.
    public class DashboardEventDto
    {
        public int EventId { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public bool IsUpcoming { get; set; }

        public int TotalEnrolments { get; set; }

        public int PendingEnrolments { get; set; }

        public int ConfirmedEnrolments { get; set; }

        public int CompletedEnrolments { get; set; }

        public int CancelledEnrolments { get; set; }
    }
}