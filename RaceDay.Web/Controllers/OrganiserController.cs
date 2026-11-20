/*
   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for applying the RequireRole filter so that only
   Organisers can open Organiser pages.


   TITLE: Make HTTP requests using IHttpClientFactory in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for calling the RaceDay API from the Organiser
   dashboard through the typed client.
*/

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Organiser pages. Only logged-in Organisers can open them.
    [RequireRole("Organiser")]
    public class OrganiserController : RaceDayControllerBase
    {
        private readonly IRaceDayApiClient _apiClient;

        public OrganiserController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // The Organiser dashboard: totals and a table of owned events.
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            ApiResult<OrganiserDashboardDto> result =
                await _apiClient.GetAsync<OrganiserDashboardDto>(
                    "api/organiser/dashboard", cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.ErrorMessage;

                return View(new OrganiserDashboardDto());
            }

            return View(result.Data);
        }
    }
}