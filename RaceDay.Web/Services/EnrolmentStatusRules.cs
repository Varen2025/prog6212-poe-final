/*
   TITLE: Selection statements - C# reference
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/selection-statements
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the switch statement that decides which
   enrolment status changes are allowed.
*/

using System;
using System.Collections.Generic;

namespace RaceDay.Web.Services
{
    // The single place that decides which status an enrolment may
    // move to. The controller enforces it and the page uses it to
    // decide which buttons to show. The API accepts any status, so
    // these rules keep the Organiser's workflow sensible.
    public static class EnrolmentStatusRules
    {
        public static IReadOnlyList<string> GetAllowedNextStatuses(
            string currentStatus,
            DateTime eventDate)
        {
            bool eventStarted = eventDate <= DateTime.Now;

            List<string> allowed = new List<string>();

            switch (currentStatus)
            {
                case "Pending":
                    // Enrolment can only be confirmed before the start.
                    if (!eventStarted)
                    {
                        allowed.Add("Confirmed");
                    }

                    allowed.Add("Cancelled");
                    break;

                case "Confirmed":
                    // An enrolment is completed once the event has begun.
                    if (eventStarted)
                    {
                        allowed.Add("Completed");
                    }

                    allowed.Add("Cancelled");
                    break;

                    // Completed and Cancelled are final.
            }

            return allowed;
        }
    }
}