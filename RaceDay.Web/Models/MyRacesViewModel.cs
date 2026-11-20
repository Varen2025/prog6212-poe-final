/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing a Participant's upcoming and past
   enrolments to the My Races page in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // A Participant's enrolments split into upcoming and past events.
    public class MyRacesViewModel
    {
        public IReadOnlyList<EnrolmentResponseDto> Upcoming { get; set; } =
            new List<EnrolmentResponseDto>();

        public IReadOnlyList<EnrolmentResponseDto> Past { get; set; } =
            new List<EnrolmentResponseDto>();
    }
}