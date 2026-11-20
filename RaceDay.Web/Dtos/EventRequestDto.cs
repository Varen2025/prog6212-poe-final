/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for the event request body that this MVC copy of
   the DTO sends to the RaceDay API.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The details sent to POST api/events and PUT api/events/{id}.
    public class EventRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        public string EventType { get; set; } = string.Empty;
    }
}