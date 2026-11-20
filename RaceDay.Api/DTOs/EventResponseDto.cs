
using System;

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

       Usage: Referenced for returning structured event
       information from RaceDay API endpoints.

       ============================================================ */

    // Defines the event information returned by the API.
    public class EventResponseDto
    {
        // Unique event identifier.
        public int EventId { get; set; }

        // ID of the Organiser who created the event.
        public int OrganiserId { get; set; }

        // Display name of the event.
        public string Name { get; set; } = string.Empty;

        // Full event description.
        public string Description { get; set; } = string.Empty;

        // Date and starting time.
        public DateTime EventDate { get; set; }

        // Event location.
        public string Location { get; set; } = string.Empty;

        // Distance in kilometres.
        public decimal DistanceKm { get; set; }

        // Run, Walk or Cycle.
        public string EventType { get; set; } = string.Empty;

        // Optional Azure banner image URL.
        public string? BannerImageUrl { get; set; }

        // Date and time the event record was created.
        public DateTime CreatedAt { get; set; }
    }
}
