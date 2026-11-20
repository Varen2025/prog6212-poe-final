/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute routes used by the result
   pages, such as Organiser/Events/2/Results/Record/1.


   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for restricting the result pages to Organisers
   with the RequireRole filter.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for protecting the record, edit and publish
   forms with anti-forgery tokens.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Record, correct and publish results for one event. Only
    // Organisers can use these pages, and only for events they own.
    [RequireRole("Organiser")]
    [Route("Organiser/Events/{eventId:int}/Results")]
    public class OrganiserResultsController : RaceDayControllerBase
    {
        // API field names that have a matching form field.
        private static readonly HashSet<string> ResultFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FinishTime", "FinishingPosition"
            };

        private readonly IRaceDayApiClient _apiClient;

        public OrganiserResultsController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: LIST
           ======================================================== */

        [HttpGet("")]
        public async Task<IActionResult> Index(
            int eventId,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? eventRedirect) =
                await GetOwnedEventAsync(
                    _apiClient, eventId, cancellationToken);

            if (eventRedirect != null)
            {
                return eventRedirect;
            }

            (List<EnrolmentResponseDto>? enrolments,
                IActionResult? enrolmentRedirect) =
                await LoadEnrolmentsAsync(eventId, cancellationToken);

            if (enrolmentRedirect != null)
            {
                return enrolmentRedirect;
            }

            (List<ResultResponseDto>? results,
                IActionResult? resultRedirect) =
                await LoadResultsAsync(eventId, cancellationToken);

            if (resultRedirect != null)
            {
                return resultRedirect;
            }

            // Only completed enrolments can have a result.
            List<ResultRowViewModel> rows = enrolments!
                .Where(enrolment => enrolment.Status == "Completed")
                .OrderBy(enrolment => enrolment.ParticipantName)
                .Select(enrolment => new ResultRowViewModel
                {
                    Enrolment = enrolment,
                    Result = results!.FirstOrDefault(
                        result => result.EnrolmentId ==
                            enrolment.EnrolmentId)
                })
                .ToList();

            ResultsListViewModel model = new ResultsListViewModel
            {
                Event = item!,
                Rows = rows,
                CompletedCount = rows.Count,
                RecordedCount = rows.Count(row => row.Result != null),
                PublishedCount = rows.Count(
                    row => row.Result != null && row.Result.IsPublished)
            };

            return View(model);
        }


        /* ========================================================
           SECTION 2: RECORD A RESULT
           ======================================================== */

        [HttpGet("Record/{enrolmentId:int}")]
        public async Task<IActionResult> Record(
            int eventId,
            int enrolmentId,
            CancellationToken cancellationToken)
        {
            (EnrolmentResponseDto? enrolment,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadRecordableEnrolmentAsync(
                    eventId, enrolmentId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ResultFormViewModel model = new ResultFormViewModel
            {
                EventId = eventId,
                EnrolmentId = enrolmentId,
                EventName = item!.Name,
                ParticipantName = enrolment!.ParticipantName,
                CategoryName = enrolment.CategoryName
            };

            return View(model);
        }

        [HttpPost("Record/{enrolmentId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(
            int eventId,
            int enrolmentId,
            ResultFormViewModel model,
            CancellationToken cancellationToken)
        {
            (EnrolmentResponseDto? enrolment,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadRecordableEnrolmentAsync(
                    eventId, enrolmentId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            // The route decides which enrolment the result is for.
            model.EventId = eventId;
            model.EnrolmentId = enrolmentId;
            model.EventName = item!.Name;
            model.ParticipantName = enrolment!.ParticipantName;
            model.CategoryName = enrolment.CategoryName;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<ResultResponseDto> result =
                await _apiClient
                    .PostAsync<ResultRequestDto, ResultResponseDto>(
                        "api/results",
                        new ResultRequestDto
                        {
                            EnrolmentId = enrolmentId,
                            FinishTime = model.ParseFinishTime()!.Value,
                            FinishingPosition =
                                model.FinishingPosition!.Value,
                            IsPublished = model.IsPublished
                        },
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                // Includes the API's messages, for example when the
                // event has not started or a result already exists.
                AddApiErrors(result, ResultFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The result for " + enrolment.ParticipantName +
                " was recorded.";

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 3: EDIT A RESULT
           ======================================================== */

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(
            int eventId,
            int id,
            CancellationToken cancellationToken)
        {
            (ResultResponseDto? existing,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadEditableResultAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ResultFormViewModel model = new ResultFormViewModel
            {
                EventId = eventId,
                EnrolmentId = existing!.EnrolmentId,
                ResultId = existing.ResultId,
                EventName = item!.Name,
                ParticipantName = existing.ParticipantName,
                CategoryName = existing.CategoryName,
                FinishTime = existing.FinishTime.ToString(
                    "HH:mm:ss", CultureInfo.InvariantCulture),
                FinishingPosition = existing.FinishingPosition,
                IsPublished = existing.IsPublished
            };

            return View(model);
        }

        [HttpPost("{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int eventId,
            int id,
            ResultFormViewModel model,
            CancellationToken cancellationToken)
        {
            (ResultResponseDto? existing,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadEditableResultAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            // The route decides which result is changed.
            model.EventId = eventId;
            model.ResultId = id;
            model.EnrolmentId = existing!.EnrolmentId;
            model.EventName = item!.Name;
            model.ParticipantName = existing.ParticipantName;
            model.CategoryName = existing.CategoryName;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<ResultResponseDto> result =
                await _apiClient
                    .PutAsync<UpdateResultRequestDto, ResultResponseDto>(
                        "api/results/" + id,
                        new UpdateResultRequestDto
                        {
                            FinishTime = model.ParseFinishTime()!.Value,
                            FinishingPosition =
                                model.FinishingPosition!.Value,
                            IsPublished = model.IsPublished
                        },
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (result.IsNotFound)
            {
                TempData["ErrorMessage"] =
                    "That result could not be found.";

                return RedirectToAction("Index", new { eventId });
            }

            if (!result.Success)
            {
                AddApiErrors(result, ResultFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The result for " + existing.ParticipantName +
                " was updated.";

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 4: PUBLISH OR UNPUBLISH
           ======================================================== */

        [HttpPost("{id:int}/Publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(
            int eventId,
            int id,
            bool publish,
            CancellationToken cancellationToken)
        {
            (ResultResponseDto? existing,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadEditableResultAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            // The API replaces all three values, so send the current
            // time and position back with the new publish setting.
            ApiResult<ResultResponseDto> result =
                await _apiClient
                    .PutAsync<UpdateResultRequestDto, ResultResponseDto>(
                        "api/results/" + id,
                        new UpdateResultRequestDto
                        {
                            FinishTime = existing!.FinishTime,
                            FinishingPosition = existing.FinishingPosition,
                            IsPublished = publish
                        },
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                return RedirectToAction("Index", new { eventId });
            }

            TempData["SuccessMessage"] =
                "The result for " + existing.ParticipantName +
                (publish ? " is now published." : " is now unpublished.");

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 5: HELPERS
           ======================================================== */

        // Gets the event's enrolments. Returns data or a redirect.
        private async Task<(List<EnrolmentResponseDto>? Data,
            IActionResult? Redirect)>
            LoadEnrolmentsAsync(
                int eventId,
                CancellationToken cancellationToken)
        {
            ApiResult<List<EnrolmentResponseDto>> result =
                await _apiClient.GetAsync<List<EnrolmentResponseDto>>(
                    "api/events/" + eventId + "/enrolments",
                    cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return (null, authRedirect);
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                return (null, RedirectToAction("Index", "Organiser"));
            }

            return (result.Data ?? new List<EnrolmentResponseDto>(), null);
        }

        // Gets the event's results. Returns data or a redirect.
        private async Task<(List<ResultResponseDto>? Data,
            IActionResult? Redirect)>
            LoadResultsAsync(
                int eventId,
                CancellationToken cancellationToken)
        {
            ApiResult<List<ResultResponseDto>> result =
                await _apiClient.GetAsync<List<ResultResponseDto>>(
                    "api/events/" + eventId + "/results",
                    cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return (null, authRedirect);
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                return (null, RedirectToAction("Index", "Organiser"));
            }

            return (result.Data ?? new List<ResultResponseDto>(), null);
        }

        // Confirms the enrolment is Completed and has no result yet.
        // If a result already exists, sends the Organiser to edit it.
        private async Task<(EnrolmentResponseDto? Enrolment,
            EventResponseDto? Item,
            IActionResult? Redirect)>
            LoadRecordableEnrolmentAsync(
                int eventId,
                int enrolmentId,
                CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? eventRedirect) =
                await GetOwnedEventAsync(
                    _apiClient, eventId, cancellationToken);

            if (eventRedirect != null)
            {
                return (null, null, eventRedirect);
            }

            (List<EnrolmentResponseDto>? enrolments,
                IActionResult? enrolmentRedirect) =
                await LoadEnrolmentsAsync(eventId, cancellationToken);

            if (enrolmentRedirect != null)
            {
                return (null, null, enrolmentRedirect);
            }

            EnrolmentResponseDto? enrolment = enrolments!
                .FirstOrDefault(entry => entry.EnrolmentId == enrolmentId);

            if (enrolment == null || enrolment.Status != "Completed")
            {
                TempData["ErrorMessage"] =
                    "Results can only be recorded for completed " +
                    "enrolments.";

                return (null, null,
                    RedirectToAction("Index", new { eventId }));
            }

            (List<ResultResponseDto>? results,
                IActionResult? resultRedirect) =
                await LoadResultsAsync(eventId, cancellationToken);

            if (resultRedirect != null)
            {
                return (null, null, resultRedirect);
            }

            ResultResponseDto? existing = results!
                .FirstOrDefault(entry => entry.EnrolmentId == enrolmentId);

            if (existing != null)
            {
                TempData["ErrorMessage"] =
                    "This enrolment already has a result. " +
                    "You can correct it here.";

                return (null, null,
                    RedirectToAction(
                        "Edit",
                        new { eventId, id = existing.ResultId }));
            }

            return (enrolment, item, null);
        }

        // Finds a result among the event's results.
        private async Task<(ResultResponseDto? Result,
            EventResponseDto? Item,
            IActionResult? Redirect)>
            LoadEditableResultAsync(
                int eventId,
                int id,
                CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? eventRedirect) =
                await GetOwnedEventAsync(
                    _apiClient, eventId, cancellationToken);

            if (eventRedirect != null)
            {
                return (null, null, eventRedirect);
            }

            (List<ResultResponseDto>? results,
                IActionResult? resultRedirect) =
                await LoadResultsAsync(eventId, cancellationToken);

            if (resultRedirect != null)
            {
                return (null, null, resultRedirect);
            }

            ResultResponseDto? existing = results!
                .FirstOrDefault(entry => entry.ResultId == id);

            if (existing == null)
            {
                TempData["ErrorMessage"] =
                    "That result could not be found.";

                return (null, null,
                    RedirectToAction("Index", new { eventId }));
            }

            return (existing, item, null);
        }
    }
}