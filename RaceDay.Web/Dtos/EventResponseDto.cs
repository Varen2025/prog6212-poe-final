/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for the shape of the event data returned by the
   RaceDay API, which this MVC copy of the DTO mirrors.
*/



using System;

namespace RaceDay.Web.Dtos
{
    // Mirrors the JSON returned by the API for an event.
    // MVC keeps its own copy so it never references the API project.
    public class EventResponseDto
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}