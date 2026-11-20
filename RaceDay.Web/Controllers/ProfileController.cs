/*
   TITLE: Routing to controller actions in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for the attribute route used by the profile page.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for checking that a user is logged in and for
   refreshing the stored first name after the profile is updated.


   TITLE: Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in
   ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for protecting the profile form with an
   anti-forgery token.


   TITLE: Upload files in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for receiving a profile picture and forwarding it
   to the RaceDay API.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Dtos;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    // View and update the logged-in user's own profile. The API only
    // ever reads and changes the account that owns the session.
    [Route("Profile")]
    public class ProfileController : RaceDayControllerBase
    {
        // API field names that have a matching form field.
        private static readonly HashSet<string> ProfileFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FirstName", "LastName", "PhoneNumber", "DateOfBirth"
            };

        private readonly IRaceDayApiClient _apiClient;

        public ProfileController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }


        /* ========================================================
           SECTION 1: VIEW THE PROFILE
           ======================================================== */

        [HttpGet("")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            IActionResult? loginRedirect = RequireLogin();

            if (loginRedirect != null)
            {
                return loginRedirect;
            }

            (ProfileResponseDto? profile, IActionResult? redirect) =
                await LoadProfileAsync(cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            ProfileFormViewModel model = new ProfileFormViewModel
            {
                FirstName = profile!.FirstName,
                LastName = profile.LastName,
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth.ToString(
                    "yyyy-MM-dd", CultureInfo.InvariantCulture)
            };

            FillDisplayFields(model, profile);

            return View(model);
        }


        /* ========================================================
           SECTION 2: UPDATE THE PROFILE
           ======================================================== */

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            ProfileFormViewModel model,
            CancellationToken cancellationToken)
        {
            IActionResult? loginRedirect = RequireLogin();

            if (loginRedirect != null)
            {
                return loginRedirect;
            }

            // Load the saved profile so the read-only details are
            // shown correctly even when the form has errors.
            (ProfileResponseDto? profile, IActionResult? redirect) =
                await LoadProfileAsync(cancellationToken);

            if (redirect != null)
            {
                return redirect;
            }

            FillDisplayFields(model, profile!);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResult<ProfileResponseDto> result =
                await _apiClient
                    .PutAsync<UpdateProfileRequestDto, ProfileResponseDto>(
                        "api/users/me",
                        new UpdateProfileRequestDto
                        {
                            FirstName = model.FirstName.Trim(),
                            LastName = model.LastName.Trim(),
                            PhoneNumber = model.PhoneNumber.Trim(),
                            DateOfBirth = model.ParseDateOfBirth()!.Value
                        },
                        cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                AddApiErrors(result, ProfileFields);
                return View(model);
            }

            // Keep the stored first name in step with the change.
            if (result.Data != null)
            {
                HttpContext.Session.SetString(
                    SessionKeys.FirstName,
                    result.Data.FirstName);
            }

            TempData["SuccessMessage"] = "Your profile was updated.";

            return RedirectToAction("Index");
        }


        /* ========================================================
           SECTION 3: UPLOAD A PROFILE PICTURE
           ======================================================== */

        // The form's file input is named "picture". The file is sent
        // to the API, which stores it in Azure Blob Storage.
        [HttpPost("Picture")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(
            IFormFile? picture,
            CancellationToken cancellationToken)
        {
            IActionResult? loginRedirect = RequireLogin();

            if (loginRedirect != null)
            {
                return loginRedirect;
            }

            // Friendly checks before anything is sent to the API.
            string? error = ImageFileRules.ValidateRequired(picture);

            if (error != null)
            {
                TempData["ErrorMessage"] = error;

                return RedirectToAction("Index");
            }

            ApiResult<ProfileImageResponseDto> result =
                await _apiClient.PostFileAsync<ProfileImageResponseDto>(
                    "api/users/me/profile-image",
                    picture!,
                    cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;

                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Your profile picture was updated.";

            return RedirectToAction("Index");
        }


        /* ========================================================
           SECTION 4: HELPERS
           ======================================================== */

        // Sends a guest to the login page.
        private IActionResult? RequireLogin()
        {
            if (HttpContext.Session.GetInt32(SessionKeys.UserId) == null)
            {
                TempData["ErrorMessage"] =
                    "Please log in to view your profile.";

                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        // Gets the logged-in user's profile from the API.
        private async Task<(ProfileResponseDto? Data,
            IActionResult? Redirect)>
            LoadProfileAsync(CancellationToken cancellationToken)
        {
            ApiResult<ProfileResponseDto> result =
                await _apiClient.GetAsync<ProfileResponseDto>(
                    "api/users/me", cancellationToken);

            IActionResult? authRedirect = RedirectOnAuthFailure(result);

            if (authRedirect != null)
            {
                return (null, authRedirect);
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.IsNotFound
                    ? "Your profile could not be found."
                    : result.ErrorMessage;

                return (null, RedirectToAction("Index", "Home"));
            }

            return (result.Data, null);
        }

        // Copies the read-only details onto the form model.
        private static void FillDisplayFields(
            ProfileFormViewModel model,
            ProfileResponseDto profile)
        {
            model.DisplayName =
                profile.FirstName + " " + profile.LastName;

            model.Email = profile.Email;
            model.Role = profile.Role;
            model.ProfileImageUrl = profile.ProfileImageUrl;
            model.CreatedAt = profile.CreatedAt;
        }
    }
}