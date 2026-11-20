/*
   TITLE: Filters in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for creating an authorization filter that checks
   the logged-in user's role in session state before an action runs.
*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters
{
    // Protects a controller or action on the server.
    // [RequireRole("Organiser")] allows only Organisers.
    // [RequireRole] with no roles allows any logged-in user.
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = false)]
    public sealed class RequireRoleAttribute
        : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RequireRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnAuthorizationAsync(
            AuthorizationFilterContext context)
        {
            ISession session = context.HttpContext.Session;

            await session.LoadAsync(context.HttpContext.RequestAborted);

            int? userId = session.GetInt32(SessionKeys.UserId);
            string? role = session.GetString(SessionKeys.Role);

            // Not logged in: send the visitor to the login page.
            if (userId == null || string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult(
                    "Login", "Account", null);

                return;
            }

            // Logged in with the wrong role: show the access denied page.
            if (_allowedRoles.Length > 0 &&
                !_allowedRoles.Contains(role, StringComparer.Ordinal))
            {
                context.Result = new RedirectToActionResult(
                    "Denied", "Access", null);

                return;
            }

            // Stop the browser keeping a copy of protected pages, so the
            // Back button after logout cannot show them.
            context.HttpContext.Response.Headers.CacheControl = "no-store";
        }
    }
}