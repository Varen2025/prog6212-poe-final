/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for the dashboard response returned by the
   RaceDay API, which this MVC copy of the DTO mirrors.
*/

using System;
using System.Collections.Generic;

namespace RaceDay.Web.Dtos
{
    // The full Organiser dashboard returned by the API.
    public class OrganiserDashboardDto
    {
        public int OrganiserId { get; set; }

        public int TotalEvents { get; set; }

        public int UpcomingEvents { get; set; }

        public int PastEvents { get; set; }

        public int TotalEnrolments { get; set; }

        public DateTime? NextUpcomingEventDate { get; set; }

        public List<DashboardEventDto> Events { get; set; }
            = new List<DashboardEventDto>();
    }
}