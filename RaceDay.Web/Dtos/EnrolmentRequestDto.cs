/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the enrolment request body that this MVC
   copy of the DTO sends to POST api/enrolments.
*/

namespace RaceDay.Web.Dtos
{
    // The details sent when a Participant enrols in an event.
    public class EnrolmentRequestDto
    {
        public int EventId { get; set; }

        public int CategoryId { get; set; }
    }
}