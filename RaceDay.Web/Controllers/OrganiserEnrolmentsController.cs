/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute routes used by the enrolment
   pages, such as Organiser/Events/2/Enrolments/5/Status.


   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for restricting the enrolment pages to
   Organisers with the RequireRole filter.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for protecting the status change forms with
   anti-forgery tokens.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // View and manage the enrolments of one event. Only Organisers
    // can use these pages, and only for events they own.
    [RequireRole("Organiser")]
    [Route("Organiser/Events/{eventId:int}/Enrolments")]
    public class OrganiserEnrolmentsController : RaceDayControllerBase
    {
        // The statuses the filter accepts.
        private static readonly string[] Statuses =
            { "Pending", "Confirmed", "Completed", "Cancelled" };

        private readonly IRaceDayApiClient _apiClient;

        public OrganiserEnrolmentsController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: LIST
           ======================================================== */

        [HttpGet("")]
        public async Task<IActionResult> Index(
            int eventId,
            string? status,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ApiResult<List<EnrolmentResponseDto>> result =
                await _apiClient.GetAsync<List<EnrolmentResponseDto>>(
                    "api/events/" + eventId + "/enrolments",
                    cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                return RedirectToAction("Index", "Organiser");
            }

            List<EnrolmentResponseDto> all =
                result.Data ?? new List<EnrolmentResponseDto>();

            // Ignore a filter value that is not a real status.
            string filter = status != null &&
                Array.IndexOf(Statuses, status) >= 0
                    ? status
                    : string.Empty;

            List<EnrolmentResponseDto> visible = filter.Length == 0
                ? all
                : all.Where(enrolment => enrolment.Status == filter)
                    .ToList();

            EnrolmentListViewModel model = new EnrolmentListViewModel
            {
                Event = item!,
                Enrolments = visible,
                Filter = filter,
                TotalCount = all.Count,
                PendingCount = all.Count(e => e.Status == "Pending"),
                ConfirmedCount = all.Count(e => e.Status == "Confirmed"),
                CompletedCount = all.Count(e => e.Status == "Completed"),
                CancelledCount = all.Count(e => e.Status == "Cancelled")
            };

            return View(model);
        }


        /* ========================================================
           SECTION 2: CHANGE STATUS
           ======================================================== */

        [HttpPost("{id:int}/Status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int eventId,
            int id,
            string? newStatus,
            string? filter,
            CancellationToken cancellationToken)
        {
            // An empty filter must not appear in the redirect URL.
            string? returnFilter =
                string.IsNullOrEmpty(filter) ? null : filter;

            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            // Reload the enrolments so the rules use the real,
            // current status and never a value from the browser.
            ApiResult<List<EnrolmentResponseDto>> listResult =
                await _apiClient.GetAsync<List<EnrolmentResponseDto>>(
                    "api/events/" + eventId + "/enrolments",
                    cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(listResult);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!listResult.Success)
            {
                TempData["ErrorMessage"] = listResult.ErrorMessage;

                return RedirectToAction(
                    "Index", new { eventId, status = returnFilter });
            }

            EnrolmentResponseDto? enrolment = listResult.Data?
                .FirstOrDefault(entry => entry.EnrolmentId == id);

            if (enrolment == null)
            {
                TempData["ErrorMessage"] =
                    "That enrolment could not be found.";

                return RedirectToAction(
                    "Index", new { eventId, status = returnFilter });
            }

            IReadOnlyList<string> allowed =
                EnrolmentStatusRules.GetAllowedNextStatuses(
                    enrolment.Status, item!.EventDate);

            if (string.IsNullOrWhiteSpace(newStatus) ||
                !allowed.Contains(newStatus))
            {
                TempData["ErrorMessage"] =
                    "That change is not allowed for an enrolment " +
                    "that is " + enrolment.Status + ".";

                return RedirectToAction(
                    "Index", new { eventId, status = returnFilter });
            }

            ApiResult<EnrolmentResponseDto> updateResult =
                await _apiClient
                    .PutAsync<UpdateEnrolmentStatusDto,
                        EnrolmentResponseDto>(
                        "api/enrolments/" + id + "/status",
                        new UpdateEnrolmentStatusDto
                        {
                            Status = newStatus
                        },
                        cancellationToken);

            authRedirect = RedirectOnAuthFailure(updateResult);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!updateResult.Success)
            {
                TempData["ErrorMessage"] = updateResult.ErrorMessage;

                return RedirectToAction(
                    "Index", new { eventId, status = returnFilter });
            }

            TempData["SuccessMessage"] =
                enrolment.ParticipantName + "'s enrolment " +
                Describe(newStatus) + ".";

            return RedirectToAction(
                "Index", new { eventId, status = returnFilter });
        }


        /* ========================================================
           SECTION 3: HELPERS
           ======================================================== */

        // Turns a status into the words used in the success message.
        private static string Describe(string status)
        {
            return status switch
            {
                "Confirmed" => "was confirmed",
                "Completed" => "was marked as completed",
                "Cancelled" => "was cancelled",
                _ => "was updated"
            };
        }

        // Loads an event and confirms the logged-in Organiser owns it.
        // Returns either the event or a redirect, never both.
        private async Task<(EventResponseDto? Item, IActionResult? Redirect)>
            LoadOwnedEventAsync(
                int eventId,
                CancellationToken cancellationToken)
        {
            ApiResult<EventResponseDto> result =
                await _apiClient.GetAsync<EventResponseDto>(
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