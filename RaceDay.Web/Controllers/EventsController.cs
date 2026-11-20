/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute routes used by the browse,
   details and enrol actions.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for reading the logged-in user's role from the
   session so the Participant-only actions are enforced on the server.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for protecting the enrol form with an
   anti-forgery token.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Pages for browsing upcoming events and enrolling in them.
    // Guests can look around. Enrolling needs a Participant login.
    [Route("Events")]
    public class EventsController : RaceDayControllerBase
    {
        // The event types the API accepts as a filter.
        private static readonly string[] EventTypes =
            { "Run", "Walk", "Cycle" };

        private readonly IRaceDayApiClient _apiClient;

        public EventsController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: BROWSE UPCOMING EVENTS
           ======================================================== */

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? eventType,
            string? location,
            string? date,
            CancellationToken cancellationToken)
        {
            EventBrowseViewModel model = new EventBrowseViewModel();
            List<string> queryParts = new List<string>();

            // Only pass a type the API understands.
            string? matchedType = EventTypes.FirstOrDefault(type =>
                string.Equals(
                    type,
                    eventType?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (matchedType != null)
            {
                model.EventType = matchedType;
                queryParts.Add("eventType=" + matchedType);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                model.Location = location.Trim();
                queryParts.Add(
                    "location=" + Uri.EscapeDataString(model.Location));
            }

            if (!string.IsNullOrWhiteSpace(date))
            {
                if (DateOnly.TryParseExact(
                        date.Trim(),
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateOnly parsedDate))
                {
                    model.Date = parsedDate.ToString(
                        "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    queryParts.Add("date=" + model.Date);
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "Enter the date as year-month-day, " +
                        "for example 2026-12-05.";
                }
            }

            string url = "api/events";

            if (queryParts.Count > 0)
            {
                url += "?" + string.Join("&", queryParts);
            }

            ApiResult<List<EventResponseDto>> result =
                await _apiClient.GetAsync<List<EventResponseDto>>(
                    url, cancellationToken);

            if (!result.Success)
            {
                ViewData["LoadError"] = result.ErrorMessage;
                return View(model);
            }

            model.Events = result.Data ?? new List<EventResponseDto>();

            return View(model);
        }


        /* ========================================================
           SECTION 2: EVENT DETAILS WITH CATEGORIES
           ======================================================== */

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            ApiResult<EventResponseDto> eventResult =
                await _apiClient.GetAsync<EventResponseDto>(
                    "api/events/" + id, cancellationToken);

            if (!eventResult.Success)
            {
                TempData["ErrorMessage"] = eventResult.IsNotFound
                    ? "That event could not be found."
                    : eventResult.ErrorMessage;

                return RedirectToAction("Index");
            }

            ApiResult<List<CategoryResponseDto>> categoryResult =
                await _apiClient.GetAsync<List<CategoryResponseDto>>(
                    "api/events/" + id + "/categories",
                    cancellationToken);

            if (!categoryResult.Success)
            {
                TempData["ErrorMessage"] = categoryResult.ErrorMessage;
                return RedirectToAction("Index");
            }

            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            EventDetailsViewModel model = new EventDetailsViewModel
            {
                Event = eventResult.Data!,
                Categories = categoryResult.Data ??
                    new List<CategoryResponseDto>(),
                Role = role
            };

            // A Participant may already be enrolled in this event.
            if (role == "Participant")
            {
                ApiResult<List<EnrolmentResponseDto>> myEnrolments =
                    await _apiClient.GetAsync<List<EnrolmentResponseDto>>(
                        "api/enrolments/me", cancellationToken);

                IActionResult? authRedirect =
                    RedirectOnAuthFailure(myEnrolments);

                if (authRedirect != null)
                {
                    return authRedirect;
                }

                if (myEnrolments.Success)
                {
                    model.MyEnrolment = myEnrolments.Data?
                        .FirstOrDefault(entry => entry.EventId == id);
                }
            }

            return View(model);
        }


        /* ========================================================
           SECTION 3: ENROL IN AN EVENT
           ======================================================== */

        [HttpPost("{id:int}/Enrol")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(
            int id,
            int categoryId,
            CancellationToken cancellationToken)
        {
            // Only a logged-in Participant may enrol. This is checked
            // here as well as by the API, so it never relies on the
            // button being hidden.
            string? role = HttpContext.Session.GetString(SessionKeys.Role);

            if (role == null)
            {
                TempData["ErrorMessage"] =
                    "Please log in as a Participant to enrol.";

                return RedirectToAction("Login", "Account");
            }

            if (role != "Participant")
            {
                return RedirectToAction("Denied", "Access");
            }

            if (categoryId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Choose a category before enrolling.";

                return RedirectToAction("Details", new { id });
            }

            ApiResult<EnrolmentResponseDto> result =
                await _apiClient
                    .PostAsync<EnrolmentRequestDto, EnrolmentResponseDto>(
                        "api/enrolments",
                        new EnrolmentRequestDto
                        {
                            EventId = id,
                            CategoryId = categoryId
                        },
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                // Shows the API's own message, for example about the
                // age requirement, a closed event or a duplicate.
                TempData["ErrorMessage"] =
                    result.ValidationErrors.Count > 0
                        ? string.Join(
                            " ",
                            result.ValidationErrors
                                .SelectMany(entry => entry.Value))
                        : result.ErrorMessage;

                return RedirectToAction("Details", new { id });
            }

            TempData["SuccessMessage"] =
                "You are enrolled. Your enrolment is pending " +
                "confirmation by the organiser.";

            return RedirectToAction("Details", new { id });
        }
    }
}