/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing an event, its filtered enrolments
   and status counts to a view in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // An event's enrolments, ready to display.
    public class EnrolmentListViewModel
    {
        public EventResponseDto Event { get; set; } = new EventResponseDto();

        // The enrolments after the status filter has been applied.
        public IReadOnlyList<EnrolmentResponseDto> Enrolments { get; set; } =
            new List<EnrolmentResponseDto>();

        // The active filter. Empty means all statuses.
        public string Filter { get; set; } = string.Empty;

        // Counts across all enrolments, ignoring the filter.
        public int TotalCount { get; set; }

        public int PendingCount { get; set; }

        public int ConfirmedCount { get; set; }

        public int CompletedCount { get; set; }

        public int CancelledCount { get; set; }
    }
}