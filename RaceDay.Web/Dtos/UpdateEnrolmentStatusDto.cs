/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the status update body that this MVC copy
   of the DTO sends to PUT api/enrolments/{id}/status.
*/

namespace RaceDay.Web.Dtos
{
    // The new status sent to the RaceDay API.
    public class UpdateEnrolmentStatusDto
    {
        // Pending, Confirmed, Cancelled or Completed.
        public string Status { get; set; } = string.Empty;
    }
}