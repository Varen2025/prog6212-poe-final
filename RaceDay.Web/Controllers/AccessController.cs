/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for the controller that shows the access denied
   page with an HTTP 403 status code.
*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers
{
    // Shown when a logged-in user opens a page for another role.
    public class AccessController : Controller
    {
        public IActionResult Denied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }
    }
}