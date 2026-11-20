/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing an event, its categories, the
   viewer's role and any existing enrolment to the details page.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // An event with its categories, for the public details page.
    public class EventDetailsViewModel
    {
        public EventResponseDto Event { get; set; } = new EventResponseDto();

        public IReadOnlyList<CategoryResponseDto> Categories { get; set; } =
            new List<CategoryResponseDto>();

        // Null for a guest, otherwise Organiser or Participant.
        public string? Role { get; set; }

        // The logged-in Participant's enrolment in this event, if any.
        public EnrolmentResponseDto? MyEnrolment { get; set; }
    }
}