
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{
    /* ============================================================
       Code Attribution

       TITLE: Create Web APIs with ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/web-api/
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for creating profile management
       endpoints and returning appropriate HTTP responses.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting profile operations
       to authenticated RaceDay users.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for retrieving and updating
       user records asynchronously in SQL Server.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Upload files in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for receiving a multipart profile
       picture upload and storing it in blob storage.

       ============================================================ */

    // Handles profile viewing and editing for RaceDay users.
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        // Database context used to access user information.
        private readonly RaceDayDbContext _context;

        // Stores uploaded profile pictures.
        private readonly IBlobStorageService _blobStorage;

        // Receive services through dependency injection.
        public UsersController(
            RaceDayDbContext context,
            IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }


        /* ========================================================
           SECTION 1: VIEW CURRENT USER PROFILE
           ======================================================== */

        /// <summary>
        /// Returns the logged-in RaceDay user's profile.
        /// </summary>
        /// <remarks>
        /// Both Organisers and Participants can view their
        /// own profile after successful authentication.
        /// Password hashes are never returned.
        /// </remarks>
        [HttpGet("me")]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 1.1: GET AUTHENTICATED USER ID
               ==================================================== */

            // Obtain the user ID from the authenticated identity.
            string? userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            // Reject an invalid or missing user identifier.
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });
            }


            /* ====================================================
               SECTION 1.2: RETRIEVE PROFILE FROM DATABASE
               ==================================================== */

            // Retrieve only the currently logged-in user.
            // The password hash is not included.
            var profile = await _context.Users
                .AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => new
                {
                    user.UserId,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.DateOfBirth,
                    user.ProfileImageUrl,
                    user.CreatedAt,
                    Role = user.Role.RoleName
                })
                .FirstOrDefaultAsync(cancellationToken);


            /* ====================================================
               SECTION 1.3: HANDLE MISSING PROFILE
               ==================================================== */

            // Return HTTP 404 if the profile no longer exists.
            if (profile == null)
            {
                return NotFound(new
                {
                    message = "User profile not found."
                });
            }


            /* ====================================================
               SECTION 1.4: RETURN HTTP 200 OK
               ==================================================== */

            // Return the authenticated user's profile.
            return Ok(profile);
        }


        /* ========================================================
           SECTION 2: UPDATE CURRENT USER PROFILE
           ======================================================== */

        /// <summary>
        /// Updates the logged-in RaceDay user's personal details.
        /// </summary>
        /// <remarks>
        /// Organisers and Participants may update their own
        /// first name, last name, phone number and date of birth.
        ///
        /// Email addresses, account roles and passwords cannot
        /// be modified using this endpoint.
        /// </remarks>
        [HttpPut("me")]
        [ProducesResponseType(
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 2.1: IDENTIFY AUTHENTICATED USER
               ==================================================== */

            // Read the user ID from the verified identity.
            string? userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            // Reject requests without a valid user ID.
            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });
            }


            /* ====================================================
               SECTION 2.2: VALIDATE PROFILE INFORMATION
               ==================================================== */

            // Remove extra spaces from the supplied details.
            string firstName = request.FirstName.Trim();
            string lastName = request.LastName.Trim();
            string phoneNumber = request.PhoneNumber.Trim();

            // Reject an empty or excessively short first name.
            if (firstName.Length < 2)
            {
                ModelState.AddModelError(
                    nameof(request.FirstName),
                    "First name must contain at least 2 characters.");
            }

            // Reject an empty or excessively short surname.
            if (lastName.Length < 2)
            {
                ModelState.AddModelError(
                    nameof(request.LastName),
                    "Last name must contain at least 2 characters.");
            }

            // Check the phone number after removing spaces.
            if (phoneNumber.Length < 7)
            {
                ModelState.AddModelError(
                    nameof(request.PhoneNumber),
                    "Phone number must contain at least 7 characters.");
            }

            // DateOnly.MinValue indicates a missing date.
            if (request.DateOfBirth == DateOnly.MinValue)
            {
                ModelState.AddModelError(
                    nameof(request.DateOfBirth),
                    "Date of birth is required.");
            }

            // A date of birth cannot be in the future.
            DateOnly today = DateOnly.FromDateTime(
                DateTime.UtcNow);

            if (request.DateOfBirth > today)
            {
                ModelState.AddModelError(
                    nameof(request.DateOfBirth),
                    "Date of birth cannot be in the future.");
            }

            // Reject invalid profile information.
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }


            /* ====================================================
               SECTION 2.3: FIND CURRENT USER IN SQL SERVER
               ==================================================== */

            // Locate the authenticated user's database record.
            // Never accept another user's ID from the request body.
            User? existingUser = await _context.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(
                    user => user.UserId == userId,
                    cancellationToken);


            /* ====================================================
               SECTION 2.4: CHECK WHETHER USER EXISTS
               ==================================================== */

            // Handle an account that no longer exists.
            if (existingUser == null)
            {
                return NotFound(new
                {
                    message = "User profile not found."
                });
            }


            /* ====================================================
               SECTION 2.5: UPDATE ALLOWED USER FIELDS
               ==================================================== */

            // Update the user's first name.
            existingUser.FirstName = firstName;

            // Update the user's surname.
            existingUser.LastName = lastName;

            // Update the contact number.
            existingUser.PhoneNumber = phoneNumber;

            // Update the date of birth.
            existingUser.DateOfBirth = request.DateOfBirth;

            // Important:
            // Do not modify UserId, RoleId, Email,
            // PasswordHash, CreatedAt or ProfileImageUrl.


            /* ====================================================
               SECTION 2.6: SAVE CHANGES TO SQL SERVER
               ==================================================== */

            // Save the updated personal information.
            await _context.SaveChangesAsync(
                cancellationToken);


            /* ====================================================
               SECTION 2.7: PREPARE UPDATED PROFILE RESPONSE
               ==================================================== */

            // Return the updated account information.
            // Do not expose the password hash.
            var updatedProfile = new
            {
                existingUser.UserId,

                existingUser.FirstName,

                existingUser.LastName,

                existingUser.Email,

                existingUser.PhoneNumber,

                existingUser.DateOfBirth,

                existingUser.ProfileImageUrl,

                existingUser.CreatedAt,

                Role = existingUser.Role.RoleName,

                Message = "RaceDay profile updated successfully."
            };


            /* ====================================================
               SECTION 2.8: RETURN HTTP 200 OK
               ==================================================== */

            // Confirm that the profile was updated.
            return Ok(updatedProfile);
        }


        /* ========================================================
           SECTION 3: UPLOAD PROFILE PICTURE
           POST /api/users/me/profile-image
           ======================================================== */

        /// <summary>
        /// Uploads the logged-in user's profile picture.
        /// </summary>
        /// <remarks>
        /// Accepts JPG, PNG or WebP up to 2 MB. The image is stored
        /// in Azure Blob Storage and its URL saved on the profile.
        /// </remarks>
        [HttpPost("me/profile-image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadProfileImage(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            // SECTION 3.1: IDENTIFY THE USER

            string? userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });
            }


            // SECTION 3.2: VALIDATE THE IMAGE

            ImageCheckResult check =
                await ImageUploadValidator.ValidateAsync(
                    file,
                    cancellationToken);

            if (!check.IsValid)
            {
                return BadRequest(new { message = check.Error });
            }


            // SECTION 3.3: FIND THE USER

            User? existingUser = await _context.Users
                .FirstOrDefaultAsync(
                    user => user.UserId == userId,
                    cancellationToken);

            if (existingUser == null)
            {
                return NotFound(new
                {
                    message = "User profile not found."
                });
            }


            // SECTION 3.4: UPLOAD TO AZURE AND SAVE THE URL

            try
            {
                await using var stream = file!.OpenReadStream();

                existingUser.ProfileImageUrl =
                    await _blobStorage.UploadImageAsync(
                        stream,
                        check.ContentType,
                        "profiles",
                        check.Extension,
                        cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message =
                            "Image storage is currently unavailable."
                    });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                profileImageUrl = existingUser.ProfileImageUrl,
                message = "Profile picture updated successfully."
            });
        }
    }
}