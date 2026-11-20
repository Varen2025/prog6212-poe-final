/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute routes used by the Participant
   pages, such as Participant and Participant/Results.


   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for restricting the Participant pages to
   Participants with the RequireRole filter.
*/

using System;
using System.Collections.Generic;
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
    // A Participant's own enrolments and published results.
    // Only Participants can use these pages, and the API only ever
    // returns the logged-in Participant's own records.
    [RequireRole("Participant")]
    [Route("Participant")]
    public class ParticipantController : RaceDayControllerBase
    {
        private readonly IRaceDayApiClient _apiClient;

        public ParticipantController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: MY ENROLMENTS
           ======================================================== */

        [HttpGet("")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            ApiResult<List<EnrolmentResponseDto>> result =
                await _apiClient.GetAsync<List<EnrolmentResponseDto>>(
                    "api/enrolments/me", cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            MyRacesViewModel model = new MyRacesViewModel();

            if (!result.Success)
            {
                ViewData["LoadError"] = result.ErrorMessage;
                return View(model);
            }

            List<EnrolmentResponseDto> all =
                result.Data ?? new List<EnrolmentResponseDto>();

            DateTime now = DateTime.Now;

            // Soonest first for upcoming, most recent first for past.
            model.Upcoming = all
                .Where(entry => entry.EventDate > now)
                .OrderBy(entry => entry.EventDate)
                .ToList();

            model.Past = all
                .Where(entry => entry.EventDate <= now)
                .OrderByDescending(entry => entry.EventDate)
                .ToList();

            return View(model);
        }


        /* ========================================================
           SECTION 2: MY PUBLISHED RESULTS
           ======================================================== */

        [HttpGet("Results")]
        public async Task<IActionResult> Results(
            CancellationToken cancellationToken)
        {
            ApiResult<List<ResultResponseDto>> result =
                await _apiClient.GetAsync<List<ResultResponseDto>>(
                    "api/results/me", cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                ViewData["LoadError"] = result.ErrorMessage;

                return View(new List<ResultResponseDto>());
            }

            // The API already returns published results only.
            return View(result.Data ?? new List<ResultResponseDto>());
        }
    }
}