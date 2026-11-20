/*
   TITLE: Make HTTP requests using IHttpClientFactory in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for mapping the API's profile picture upload
   response into an MVC-side data transfer object.
*/

namespace RaceDay.Web.Dtos
{
    // What the API returns after a profile picture upload.
    public class ProfileImageResponseDto
    {
        // Public Azure Blob address of the new picture.
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Confirmation message from the API.
        public string? Message { get; set; }
    }
}