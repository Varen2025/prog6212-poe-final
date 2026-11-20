/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for combining an enrolment and its optional
   result into one row of the results list.
*/

using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // One completed enrolment and its result, if one was recorded.
    public class ResultRowViewModel
    {
        public EnrolmentResponseDto Enrolment { get; set; } =
            new EnrolmentResponseDto();

        // Null until a result has been recorded.
        public ResultResponseDto? Result { get; set; }
    }
}