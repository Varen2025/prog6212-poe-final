/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing the home page data (the next few
   upcoming events and the viewer's role) in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // The data shown on the RaceDay home page.
    public class HomeViewModel
    {
        // The next few upcoming events.
        public IReadOnlyList<EventResponseDto> UpcomingEvents { get; set; } =
            new List<EventResponseDto>();

        // Null for a guest, otherwise Organiser or Participant.
        public string? Role { get; set; }

        // Set when the events could not be loaded from the API.
        public string? LoadError { get; set; }
    }
}