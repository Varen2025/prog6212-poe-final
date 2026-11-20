/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing the event filters and the matching
   events to the browse page in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // The filters and results shown on the browse events page.
    public class EventBrowseViewModel
    {
        public string? EventType { get; set; }

        public string? Location { get; set; }

        // Kept as text (yyyy-MM-dd) from the date input.
        public string? Date { get; set; }

        public IReadOnlyList<EventResponseDto> Events { get; set; } =
            new List<EventResponseDto>();

        // True when at least one filter is in use.
        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(EventType) ||
            !string.IsNullOrWhiteSpace(Location) ||
            !string.IsNullOrWhiteSpace(Date);
    }
}