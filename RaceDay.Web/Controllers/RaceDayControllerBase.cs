/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for creating a base controller that handles
   expired sessions, forbidden responses and API validation errors
   in one place.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for showing a message through TempData after
   redirecting an expired session to the login page, and for reading
   the logged-in user's ID from the session.
*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Base class for RaceDay controllers that call the API.
    public abstract class RaceDayControllerBase : Controller
    {
        // Returns a redirect when the API rejected the user's session
        // (401 with a stored session) or refused the action (403).
        // Returns null when the result needs no special handling.
        protected IActionResult? RedirectOnAuthFailure(ApiResult result)
        {
            if (result.SessionExpired)
            {
                // The API client already cleared the MVC session.
                TempData["ErrorMessage"] =
                    "Your session has expired. Please log in again.";

                return RedirectToAction("Login", "Account");
            }

            if (result.IsForbidden)
            {
                return RedirectToAction("Denied", "Access");
            }

            return null;
        }

        // Adds the API's error messages to ModelState.
        // Field errors go beside the matching input. Anything else
        // goes into the summary at the top of the form.
        protected void AddApiErrors(
            ApiResult result,
            HashSet<string> formFields)
        {
            if (result.ValidationErrors.Count > 0)
            {
                foreach (KeyValuePair<string, string[]> entry in
                    result.ValidationErrors)
                {
                    string key = formFields.Contains(entry.Key)
                        ? entry.Key
                        : string.Empty;

                    foreach (string message in entry.Value)
                    {
                        ModelState.AddModelError(key, message);
                    }
                }

                return;
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        // Loads an event and confirms the logged-in Organiser owns it.
        // Returns either the event or a redirect, never both.
        protected async Task<(EventResponseDto? Item, IActionResult? Redirect)>
            GetOwnedEventAsync(
                IRaceDayApiClient apiClient,
                int eventId,
                CancellationToken cancellationToken)
        {
            ApiResult<EventResponseDto> result =
                await apiClient.GetAsync<EventResponseDto>(
                    "api/events/" + eventId, cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return (null, authRedirect);
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.IsNotFound
                    ? "That event could not be found."
                    : result.ErrorMessage;

                return (null, RedirectToAction("Index", "Organiser"));
            }

            int? userId = HttpContext.Session
                .GetInt32(SessionKeys.UserId);

            // An Organiser may only manage their own events.
            if (userId == null || result.Data!.OrganiserId != userId.Value)
            {
                return (null, RedirectToAction("Denied", "Access"));
            }

            return (result.Data, null);
        }
    }
}