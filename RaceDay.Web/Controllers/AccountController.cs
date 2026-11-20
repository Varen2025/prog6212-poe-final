/*
   TITLE: Model binding and validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for validating the register and login forms and
   for adding API validation messages to ModelState.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for protecting the register, login and logout
   forms with anti-forgery tokens.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for storing the logged-in user's ID, role and
   name in session state and for TempData messages.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // Handles registration, login and logout for RaceDay users.
    // All account work is done by the Part 2 API.
    public class AccountController : Controller
    {
        // API field names that have a matching form field.
        private static readonly HashSet<string> RegisterFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FirstName", "LastName", "Email", "Password",
                "PhoneNumber", "DateOfBirth", "Role"
            };

        private static readonly HashSet<string> LoginFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Email", "Password"
            };

        private readonly IRaceDayApiClient _apiClient;

        public AccountController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: REGISTER
           ======================================================== */

        [HttpGet]
        public IActionResult Register()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model,
            CancellationToken cancellationToken)
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            // The API also rejects future dates of birth.
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value > today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            RegisterRequestDto request = new RegisterRequestDto
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password,
                PhoneNumber = model.PhoneNumber.Trim(),
                DateOfBirth = model.DateOfBirth!.Value,
                Role = model.Role
            };

            ApiResult<RegisterResponseDto> result =
                await _apiClient
                    .PostAsync<RegisterRequestDto, RegisterResponseDto>(
                        "api/auth/register", request, cancellationToken);

            if (!result.Success)
            {
                AddApiErrors(result, RegisterFields);
                return View(model);
            }

            // The API does not log the user in after registration.
            TempData["SuccessMessage"] =
                "Your account was created. Please log in.";

            return RedirectToAction(nameof(Login));
        }


        /* ========================================================
           SECTION 2: LOGIN
           ======================================================== */

        [HttpGet]
        public IActionResult Login()
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            CancellationToken cancellationToken)
        {
            if (IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Start from an empty session so no old API cookie is
            // sent with the login request. The cookie handler stores
            // the new API cookie during the call below.
            await HttpContext.Session.LoadAsync(cancellationToken);
            HttpContext.Session.Clear();

            LoginRequestDto request = new LoginRequestDto
            {
                Email = model.Email.Trim(),
                Password = model.Password
            };

            ApiResult<LoginResponseDto> result =
                await _apiClient
                    .PostAsync<LoginRequestDto, LoginResponseDto>(
                        "api/auth/login", request, cancellationToken);

            if (!result.Success)
            {
                AddApiErrors(result, LoginFields);
                return View(model);
            }

            LoginResponseDto user = result.Data!;

            string? apiCookie = HttpContext.Session
                .GetString(SessionKeys.ApiSessionCookie);

            bool roleIsValid =
                user.Role == "Organiser" || user.Role == "Participant";

            // Only trust the login if the API session cookie was
            // captured and the role is one we recognise.
            if (string.IsNullOrEmpty(apiCookie) || !roleIsValid)
            {
                HttpContext.Session.Clear();

                ModelState.AddModelError(
                    string.Empty,
                    "We could not start your session. " +
                    "Please try again.");

                return View(model);
            }

            HttpContext.Session.SetInt32(
                SessionKeys.UserId, user.UserId);
            HttpContext.Session.SetString(
                SessionKeys.Role, user.Role);
            HttpContext.Session.SetString(
                SessionKeys.FirstName, user.FirstName);

            // Organisers start on their dashboard. Participants start
            // on the home page.
            if (user.Role == "Organiser")
            {
                return RedirectToAction("Index", "Organiser");
            }

            return RedirectToAction("Index", "Home");
        }


        /* ========================================================
           SECTION 3: LOGOUT
           ======================================================== */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            // Ask the API to end its session. Even if the API cannot
            // be reached, the MVC session is still cleared below.
            if (IsLoggedIn())
            {
                await _apiClient.PostWithoutBodyAsync(
                    "api/auth/logout", cancellationToken);
            }

            await HttpContext.Session.LoadAsync(cancellationToken);
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "You have been logged out.";

            return RedirectToAction(nameof(Login));
        }


        /* ========================================================
           SECTION 4: HELPERS
           ======================================================== */

        // True when this browser has a logged-in MVC session.
        private bool IsLoggedIn()
        {
            return HttpContext.Session
                .GetInt32(SessionKeys.UserId) != null;
        }

        // Adds the API's error messages to ModelState.
        // Field errors go beside the matching input. Anything else
        // goes into the summary at the top of the form.
        private void AddApiErrors(
            ApiResult result,
            HashSet<string> formFields)
        {
            if (result.ValidationErrors.Count > 0)
            {
                foreach (KeyValuePair<string, string[]> entry in
                    result.ValidationErrors)
                {
                    string key = formFields.Contains(entry.Key)
                        ? entry.Key
                        : string.Empty;

                    foreach (string message in entry.Value)
                    {
                        ModelState.AddModelError(key, message);
                    }
                }

                return;
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }
    }
}