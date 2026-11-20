/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute routes used by the category
   pages, such as Organiser/Events/2/Categories/5/Edit.


   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for restricting the category pages to Organisers
   with the RequireRole filter.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for protecting the category create, edit and
   delete forms with anti-forgery tokens.
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
    // Manage the categories of one event. Only Organisers can use
    // these pages, and only for events they own.
    [RequireRole("Organiser")]
    [Route("Organiser/Events/{eventId:int}/Categories")]
    public class OrganiserCategoriesController : RaceDayControllerBase
    {
        // API field names that have a matching form field.
        private static readonly HashSet<string> CategoryFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CategoryName", "CategoryType",
                "MinAge", "MaxAge", "DistanceKm"
            };

        private readonly IRaceDayApiClient _apiClient;

        public OrganiserCategoriesController(IRaceDayApiClient apiClient)
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
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ApiResult<List<CategoryResponseDto>> result =
                await _apiClient.GetAsync<List<CategoryResponseDto>>(
                    "api/events/" + eventId + "/categories",
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

            CategoryListViewModel model = new CategoryListViewModel
            {
                Event = item!,
                Categories = result.Data ?? new List<CategoryResponseDto>()
            };

            return View(model);
        }


        /* ========================================================
           SECTION 2: CREATE
           ======================================================== */

        [HttpGet("Create")]
        public async Task<IActionResult> Create(
            int eventId,
            CancellationToken cancellationToken)
        {
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            CategoryFormViewModel model = new CategoryFormViewModel
            {
                EventId = eventId,
                EventName = item!.Name,
                CategoryType = "Age"
            };

            return View(model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int eventId,
            CategoryFormViewModel model,
            CancellationToken cancellationToken)
        {
            // Confirms ownership and supplies the event name.
            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            model.EventId = eventId;
            model.EventName = item!.Name;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<CategoryResponseDto> result =
                await _apiClient
                    .PostAsync<CategoryRequestDto, CategoryResponseDto>(
                        "api/events/" + eventId + "/categories",
                        ToRequest(model),
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                // Includes the duplicate name message from the API.
                AddApiErrors(result, CategoryFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The category \"" + result.Data!.CategoryName +
                "\" was added.";

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 3: EDIT
           ======================================================== */

        [HttpGet("{id:int}/Edit")]
        public async Task<IActionResult> Edit(
            int eventId,
            int id,
            CancellationToken cancellationToken)
        {
            (CategoryResponseDto? category,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadOwnedCategoryAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            CategoryFormViewModel model = new CategoryFormViewModel
            {
                EventId = eventId,
                CategoryId = category!.CategoryId,
                EventName = item!.Name,
                CategoryName = category.CategoryName,
                CategoryType = category.CategoryType,
                MinAge = category.MinAge,
                MaxAge = category.MaxAge,
                DistanceKm = category.DistanceKm
            };

            return View(model);
        }

        [HttpPost("{id:int}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int eventId,
            int id,
            CategoryFormViewModel model,
            CancellationToken cancellationToken)
        {
            (CategoryResponseDto? category,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadOwnedCategoryAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            // The route decides which category is changed.
            model.EventId = eventId;
            model.CategoryId = id;
            model.EventName = item!.Name;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<CategoryResponseDto> result =
                await _apiClient
                    .PutAsync<CategoryRequestDto, CategoryResponseDto>(
                        "api/categories/" + id,
                        ToRequest(model),
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (result.IsNotFound)
            {
                TempData["ErrorMessage"] =
                    "That category could not be found.";

                return RedirectToAction("Index", new { eventId });
            }

            if (!result.Success)
            {
                AddApiErrors(result, CategoryFields);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "The category \"" + result.Data!.CategoryName +
                "\" was updated.";

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 4: DELETE
           ======================================================== */

        [HttpGet("{id:int}/Delete")]
        public async Task<IActionResult> Delete(
            int eventId,
            int id,
            CancellationToken cancellationToken)
        {
            (CategoryResponseDto? category,
                EventResponseDto? item,
                IActionResult? redirect) =
                await LoadOwnedCategoryAsync(
                    eventId, id, cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ViewBag.EventName = item!.Name;

            return View(category);
        }

        [HttpPost("{id:int}/Delete")]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int eventId,
            int id,
            CancellationToken cancellationToken)
        {
            ApiResult result = await _apiClient.DeleteAsync(
                "api/categories/" + id, cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                // Includes the API's message when an enrolment uses
                // the category.
                TempData["ErrorMessage"] = result.IsNotFound
                    ? "That category could not be found."
                    : result.ErrorMessage;

                return RedirectToAction("Index", new { eventId });
            }

            TempData["SuccessMessage"] = "The category was deleted.";

            return RedirectToAction("Index", new { eventId });
        }


        /* ========================================================
           SECTION 5: HELPERS
           ======================================================== */

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

        // Loads a category and its event, and confirms the category
        // belongs to the event in the URL and that the logged-in
        // Organiser owns that event.
        private async Task<(
            CategoryResponseDto? Category,
            EventResponseDto? Item,
            IActionResult? Redirect)>
            LoadOwnedCategoryAsync(
                int eventId,
                int id,
                CancellationToken cancellationToken)
        {
            ApiResult<CategoryResponseDto> result =
                await _apiClient.GetAsync<CategoryResponseDto>(
                    "api/categories/" + id, cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return (null, null, authRedirect);
            }

            if (!result.Success || result.Data!.EventId != eventId)
            {
                TempData["ErrorMessage"] =
                    "That category could not be found.";

                return (null, null,
                    RedirectToAction("Index", new { eventId }));
            }

            (EventResponseDto? item, IActionResult? redirect) =
                await LoadOwnedEventAsync(eventId, cancellationToken);

            if (redirect != null)
            {
                return (null, null, redirect);
            }

            return (result.Data, item, null);
        }

        // Converts the form into the request body the API expects.
        // Only the fields for the chosen type are sent.
        private static CategoryRequestDto ToRequest(
            CategoryFormViewModel model)
        {
            CategoryRequestDto request = new CategoryRequestDto
            {
                CategoryName = model.CategoryName.Trim(),
                CategoryType = model.CategoryType
            };

            if (model.CategoryType == "Age")
            {
                request.MinAge = model.MinAge;
                request.MaxAge = model.MaxAge;
            }
            else
            {
                request.DistanceKm = model.DistanceKm;
            }

            return request;
        }
    }
}