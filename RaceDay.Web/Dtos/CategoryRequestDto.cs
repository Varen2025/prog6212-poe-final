/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the category request body that this MVC
   copy of the DTO sends to the RaceDay API.
*/

namespace RaceDay.Web.Dtos
{
    // The details sent to POST api/events/{eventId}/categories and
    // PUT api/categories/{id}. Age categories leave DistanceKm null.
    // Distance categories leave both ages null.
    public class CategoryRequestDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public string CategoryType { get; set; } = string.Empty;

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        public decimal? DistanceKm { get; set; }
    }
}