/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the controller structure and for the home
   page that shows the next upcoming events from the RaceDay API.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for reading the logged-in user's role from the
   session so the home page shows the right buttons.
*/

using System.Collections.Generic;
using System.Diagnostics;
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
    public class HomeController : Controller
    {
        // How many upcoming events the home page previews.
        private const int PreviewCount = 3;

        private readonly IRaceDayApiClient _apiClient;

        public HomeController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            HomeViewModel model = new HomeViewModel
            {
                Role = HttpContext.Session.GetString(SessionKeys.Role)
            };

            // The events endpoint is public, so guests can see it too.
            ApiResult<List<EventResponseDto>> events =
                await _apiClient.GetAsync<List<EventResponseDto>>(
                    "api/events", cancellationToken);

            if (events.Success)
            {
                model.UpcomingEvents = (events.Data ??
                        new List<EventResponseDto>())
                    .Take(PreviewCount)
                    .ToList();
            }
            else
            {
                model.LoadError = events.ErrorMessage;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}