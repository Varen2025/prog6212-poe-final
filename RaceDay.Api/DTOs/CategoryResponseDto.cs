
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

       Usage: Referenced for returning structured category
       information from the RaceDay API.

       ============================================================ */

    // Defines the category information returned by the API.
    public class CategoryResponseDto
    {
        // Unique category identifier.
        public int CategoryId { get; set; }

        // Event that the category belongs to.
        public int EventId { get; set; }

        // Display name shown to Participants.
        public string CategoryName { get; set; } = string.Empty;

        // Identifies Age or Distance categories.
        public string CategoryType { get; set; } = string.Empty;

        // Minimum age for age-based categories.
        public int? MinAge { get; set; }

        // Maximum age for age-based categories.
        public int? MaxAge { get; set; }

        // Distance in kilometres for distance categories.
        public decimal? DistanceKm { get; set; }
    }
}
