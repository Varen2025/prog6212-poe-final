/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the result request body that this MVC copy
   of the DTO sends to POST api/results.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The details sent when an Organiser records a result.
    public class ResultRequestDto
    {
        public int EnrolmentId { get; set; }

        public TimeOnly FinishTime { get; set; }

        public int FinishingPosition { get; set; }

        public bool IsPublished { get; set; }
    }
}