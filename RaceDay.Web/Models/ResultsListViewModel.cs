/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing an event and its result rows to a
   view in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // An event's completed enrolments with their results.
    public class ResultsListViewModel
    {
        public EventResponseDto Event { get; set; } = new EventResponseDto();

        public IReadOnlyList<ResultRowViewModel> Rows { get; set; } =
            new List<ResultRowViewModel>();

        public int CompletedCount { get; set; }

        public int RecordedCount { get; set; }

        public int PublishedCount { get; set; }
    }
}