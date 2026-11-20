/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for the attribute routes used by the Organiser
   event pages, such as Organiser/Events/5/Edit.


   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for restricting the event pages to Organisers
   with the RequireRole filter.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for protecting the create, edit and delete forms
   with anti-forgery tokens.


   TITLE: Upload files in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for receiving the optional event banner file and
   forwarding it to the RaceDay API.
*/

using System;
using System.Collections.Generic;
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
    // Create, view, edit and delete events. Only Organisers can use
    // these pages, and only for events they own.
    [RequireRole("Organiser")]
    [Route("Organiser/Events")]
    public class OrganiserEventsController : RaceDayControllerBase
    {
        // API field names that have a matching form field.
        private static readonly HashSet<string> EventFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Name", "Description", "EventDate",
                "Location", "DistanceKm", "EventType"
            };

        private readonly IRaceDayApiClient _apiClient;

        public OrganiserEventsController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: CREATE
           ======================================================== */

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            EventFormViewModel model,
            CancellationToken cancellationToken)
        {
            // A new event must be scheduled in the future.
            if (model.EventDate.HasValue &&
                model.EventDate.Value <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.EventDate),
                    "The event date must be in the future.");
            }

            // Reject a bad banner before anything is saved.
            string? bannerError =
                ImageFileRules.ValidateOptional(model.BannerImage);

            if (bannerError != null)
            {
                ModelState.AddModelError(
                    nameof(model.BannerImage), bannerError);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<EventResponseDto> result =
                await _apiClient
                    .PostAsync<EventRequestDto, EventResponseDto>(
                        "api/events", ToRequest(model), cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                AddApiErrors(result, EventFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The event \"" + result.Data!.Name + "\" was created.";

            // The event now exists, so send the banner (if any).
            IActionResult? bannerRedirect =
                await UploadBannerIfChosenAsync(
                    result.Data.EventId,
                    model.BannerImage,
                    cancellationToken);

            if (bannerRedirect != null)
            {
                return bannerRedirect;
            }

            return RedirectToAction("Index", "Organiser");
        }


        /* ========================================================
           SECTION 2: DETAILS
           ======================================================== */

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            return View(item);
        }


        /* ========================================================
           SECTION 3: EDIT
           ======================================================== */

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            EventFormViewModel model = new EventFormViewModel
            {
                EventId = item!.EventId,
                Name = item.Name,
                Description = item.Description,
                EventDate = item.EventDate,
                Location = item.Location,
                DistanceKm = item.DistanceKm,
                EventType = item.EventType,
                CurrentBannerUrl = item.BannerImageUrl
            };

            return View(model);
        }

        [HttpPost("{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EventFormViewModel model,
            CancellationToken cancellationToken)
        {
            // The route decides which event is changed.
            model.EventId = id;

            // Reject a bad banner before anything is saved.
            string? bannerError =
                ImageFileRules.ValidateOptional(model.BannerImage);

            if (bannerError != null)
            {
                ModelState.AddModelError(
                    nameof(model.BannerImage), bannerError);
            }

            // Past dates are allowed when editing, so an Organiser can
            // close enrolment and record results for a finished event.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<EventResponseDto> result =
                await _apiClient
                    .PutAsync<EventRequestDto, EventResponseDto>(
                        "api/events/" + id,
                        ToRequest(model),
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (result.IsNotFound)
            {
                TempData["ErrorMessage"] = "That event could not be found.";

                return RedirectToAction("Index", "Organiser");
            }

            if (!result.Success)
            {
                AddApiErrors(result, EventFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The event \"" + result.Data!.Name + "\" was updated.";

            // The details are saved, so send the new banner (if any).
            IActionResult? bannerRedirect =
                await UploadBannerIfChosenAsync(
                    id,
                    model.BannerImage,
                    cancellationToken);

            if (bannerRedirect != null)
            {
                return bannerRedirect;
            }

            return RedirectToAction("Index", "Organiser");
        }


        /* ========================================================
           SECTION 4: DELETE
           ======================================================== */

        [HttpGet("{id:int}/Delete")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            return View(item);
        }

        [HttpPost("{id:int}/Delete")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id,
            CancellationToken cancellationToken)
        {
            ApiResult result = await _apiClient.DeleteAsync(
                "api/events/" + id, cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                // Includes the API's message when categories or
                // enrolments stop the event from being deleted.
                TempData["ErrorMessage"] = result.IsNotFound
                    ? "That event could not be found."
                    : result.ErrorMessage;

                return RedirectToAction("Index", "Organiser");
            }

            TempData["SuccessMessage"] = "The event was deleted.";

            return RedirectToAction("Index", "Organiser");
        }


        /* ========================================================
           SECTION 5: HELPERS
           ======================================================== */

        // Sends the chosen banner to the API. Returns null when there
        // is nothing to upload or the upload worked. Returns a redirect
        // when the session expired or the upload failed, in which case
        // the event itself is already saved.
        private async Task<IActionResult?> UploadBannerIfChosenAsync(
            int eventId,
            IFormFile? banner,
            CancellationToken cancellationToken)
        {
            if (banner == null || banner.Length == 0)
            {
                return null;
            }

            ApiResult<EventResponseDto> bannerResult =
                await _apiClient.PostFileAsync<EventResponseDto>(
                    "api/events/" + eventId + "/banner",
                    banner,
                    cancellationToken);

            IActionResult? authRedirect =
                RedirectOnAuthFailure(bannerResult);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!bannerResult.Success)
            {
                TempData["ErrorMessage"] =
                    "The event was saved, but the banner could not be " +
                    "uploaded: " + bannerResult.ErrorMessage +
                    " You can add it by editing the event.";

                return RedirectToAction("Index", "Organiser");
            }

            return null;
        }

        // Loads an event and confirms the logged-in Organiser owns it.
        // Returns either the event or a redirect, never both.
        private async Task<(EventResponseDto? Item, IActionResult? Redirect)>
            LoadOwnedEventAsync(
                int id,
                CancellationToken cancellationToken)
        {
            ApiResult<EventResponseDto> result =
                await _apiClient.GetAsync<EventResponseDto>(
                    "api/events/" + id, cancellationToken);

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

            // An Organiser may only open their own events.
            if (userId == null || result.Data!.OrganiserId != userId.Value)
            {
                return (null, RedirectToAction("Denied", "Access"));
            }

            return (result.Data, null);
        }

        // Converts the form into the request body the API expects.
        private static EventRequestDto ToRequest(EventFormViewModel model)
        {
            return new EventRequestDto
            {
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                EventDate = model.EventDate!.Value,
                Location = model.Location.Trim(),
                DistanceKm = model.DistanceKm!.Value,
                EventType = model.EventType
            };
        }
    }
}