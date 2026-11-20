/*
   TITLE: Create Web APIs with ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/web-api/
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the result update body that this MVC copy
   of the DTO sends to PUT api/results/{id}.
*/

using System;

namespace RaceDay.Web.Dtos
{
    // The details sent when an Organiser corrects or publishes a result.
    public class UpdateResultRequestDto
    {
        public TimeOnly FinishTime { get; set; }

        public int FinishingPosition { get; set; }

        public bool IsPublished { get; set; }
    }
}