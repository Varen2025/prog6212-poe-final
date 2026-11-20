/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the enrolment response body that this MVC
   copy of the DTO reads from the RaceDay API.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // An enrolment returned by the RaceDay API.
    public class EnrolmentResponseDto
    {
        public int EnrolmentId { get; set; }

        public int ParticipantId { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public DateTime EnrolmentDate { get; set; }

        // Pending, Confirmed, Cancelled or Completed.
        public string Status { get; set; } = string.Empty;
    }
}