
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for creating the RaceDay registration,
       login and logout endpoints and returning HTTP responses.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Session and State Management in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for storing the logged-in user's
       ID and role using server-side session state.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: PasswordVerificationResult Enum
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordverificationresult
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for handling password verification
       results during the login process.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for reading and saving user
       information asynchronously through Entity Framework.

       ============================================================ */

    // Handles registration, login and logout for RaceDay users.
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        // Database context used to access users and roles.
        private readonly RaceDayDbContext _context;

        // Service used for password hashing and verification.
        private readonly IPasswordService _passwordService;

        // Receive the required services through dependency injection.
        public AuthController(
            RaceDayDbContext context,
            IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }


        /* ========================================================
           SECTION 1: REGISTER A NEW RACEDAY USER
           ======================================================== */

       
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(RegisterResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<RegisterResponseDto>> Register(
            [FromBody] RegisterRequestDto request,
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 1.1: NORMALISE THE EMAIL
               ==================================================== */

            // Remove extra spaces and convert the email to lowercase.
            string email = request.Email.Trim().ToLowerInvariant();


            /* ====================================================
               SECTION 1.2: CHECK FOR DUPLICATE ACCOUNTS
               ==================================================== */

            // Check if an account already uses the email address.
            bool emailExists = await _context.Users
                .AnyAsync(
                    user => user.Email == email,
                    cancellationToken);

            // Return HTTP 409 if the email is already registered.
            if (emailExists)
            {
                return Conflict(new
                {
                    message = "This email address is already registered."
                });
            }


            /* ====================================================
               SECTION 1.3: FIND THE SELECTED USER ROLE
               ==================================================== */

            // Retrieve the selected Organiser or Participant role.
            Role? selectedRole = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    role => role.RoleName == request.Role,
                    cancellationToken);

            // The initial database migration should contain
            // both roles. Handle a missing role configuration.
            if (selectedRole == null)
            {
                return Problem(
                    title: "Registration is temporarily unavailable.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }


            /* ====================================================
               SECTION 1.4: CREATE THE USER ENTITY
               ==================================================== */

            // Map the registration details to the User model.
            User newUser = new User
            {
                // Store the selected role ID.
                RoleId = selectedRole.RoleId,

                // Store the user's first name.
                FirstName = request.FirstName.Trim(),

                // Store the user's surname.
                LastName = request.LastName.Trim(),

                // Store the normalised email address.
                Email = email,

                // Store the supplied contact number.
                PhoneNumber = request.PhoneNumber.Trim(),

                // Store the user's date of birth.
                DateOfBirth = request.DateOfBirth,

                // No profile image is provided during registration.
                ProfileImageUrl = null
            };


            /* ====================================================
               SECTION 1.5: HASH THE USER PASSWORD
               ==================================================== */

            // Generate a salted password hash using PasswordService.
            // Never store the original password as plain text.
            newUser.PasswordHash =
                _passwordService.HashPassword(
                    newUser,
                    request.Password);


            /* ====================================================
               SECTION 1.6: SAVE THE ACCOUNT TO SQL SERVER
               ==================================================== */

            // Add the new account to the database context.
            _context.Users.Add(newUser);

            try
            {
                // Save the new account asynchronously.
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException
                    sqlException &&
                    (sqlException.Number == 2601 ||
                     sqlException.Number == 2627))
            {
                // Handle duplicate email constraint violations.
                // This also protects against concurrent registrations.
                return Conflict(new
                {
                    message = "This email address is already registered."
                });
            }


            /* ====================================================
               SECTION 1.7: PREPARE THE REGISTRATION RESPONSE
               ==================================================== */

            // Return the registered user's information.
            // Do not include the password or password hash.
            RegisterResponseDto response =
                new RegisterResponseDto
                {
                    UserId = newUser.UserId,

                    FirstName = newUser.FirstName,

                    LastName = newUser.LastName,

                    Email = newUser.Email,

                    Role = selectedRole.RoleName,

                    CreatedAt = newUser.CreatedAt,

                    Message = "RaceDay account created successfully."
                };


            /* ====================================================
               SECTION 1.8: RETURN HTTP 201 CREATED
               ==================================================== */

            // Confirm that the registration was successful.
            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }


        /* ========================================================
           SECTION 2: LOGIN TO RACEDAY
           ======================================================== */

       
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(LoginResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 2.1: NORMALISE THE EMAIL
               ==================================================== */

            // Format the email consistently with registration.
            string email = request.Email.Trim().ToLowerInvariant();


            /* ====================================================
               SECTION 2.2: FIND THE REGISTERED USER
               ==================================================== */

            // Retrieve the account and its assigned role.
            User? user = await _context.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(
                    user => user.Email == email,
                    cancellationToken);


            /* ====================================================
               SECTION 2.3: CHECK WHETHER THE ACCOUNT EXISTS
               ==================================================== */

            // Use a general error message instead of revealing
            // whether a specific email address is registered.
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }


            /* ====================================================
               SECTION 2.4: VERIFY THE USER PASSWORD
               ==================================================== */

            // Compare the entered password against the stored hash.
            PasswordVerificationResult verificationResult =
                _passwordService.VerifyPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            // Reject an incorrect password.
            if (verificationResult ==
                PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }


            /* ====================================================
               SECTION 2.5: UPGRADE PASSWORD HASH IF NECESSARY
               ==================================================== */

            // ASP.NET Core Identity may indicate that an older
            // valid password hash should be upgraded.
            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                // Generate a new password hash.
                user.PasswordHash =
                    _passwordService.HashPassword(
                        user,
                        request.Password);

                // Save the updated hash in the database.
                await _context.SaveChangesAsync(
                    cancellationToken);
            }


            /* ====================================================
               SECTION 2.6: CREATE SERVER-SIDE SESSION
               ==================================================== */

            // Load the current session asynchronously.
            await HttpContext.Session.LoadAsync(
                cancellationToken);

            // Clear any previous session values.
            HttpContext.Session.Clear();

            // Store the logged-in user's ID.
            HttpContext.Session.SetInt32(
                "UserId",
                user.UserId);

            // Store the logged-in user's role.
            HttpContext.Session.SetString(
                "Role",
                user.Role.RoleName);

            // Save the session information to the server cache.
            await HttpContext.Session.CommitAsync(
                cancellationToken);


            /* ====================================================
               SECTION 2.7: PREPARE LOGIN RESPONSE
               ==================================================== */

            // Return safe user details without the password hash.
            LoginResponseDto response = new LoginResponseDto
            {
                UserId = user.UserId,

                FirstName = user.FirstName,

                LastName = user.LastName,

                Email = user.Email,

                Role = user.Role.RoleName,

                Message = "RaceDay login successful."
            };


            /* ====================================================
               SECTION 2.8: RETURN HTTP 200 OK
               ==================================================== */

            // Confirm successful login.
            return Ok(response);
        }


        /* ========================================================
           SECTION 3: LOGOUT FROM RACEDAY
           ======================================================== */

        /* ============================================================
           Code Attribution

           TITLE: Session and State Management in ASP.NET Core
           AUTHOR: Microsoft
           DATE: n.d.
           AVAILABLE:
           https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
           DATE ACCESSED: 18 October 2026

           Usage: Referenced for clearing server-side session
           state when a RaceDay user logs out.

           ============================================================ */

        /* ============================================================
           Code Attribution

           TITLE: IResponseCookies.Delete Method
           AUTHOR: Microsoft
           DATE: n.d.
           AVAILABLE:
           https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.iresponsecookies.delete
           DATE ACCESSED: 18 October 2026

           Usage: Referenced for deleting the RaceDay session
           cookie from the browser during logout.

           ============================================================ */

        /// <summary>
        /// Logs out the currently authenticated RaceDay user.
        /// </summary>
        /// <remarks>
        /// Checks the active login session, clears server-side
        /// values and removes the browser session cookie.
        /// </remarks>
        /// <param name="cancellationToken">
        /// Cancels session operations when the request is cancelled.
        /// </param>
        /// <returns>
        /// 204 No Content after successful logout.
        /// 401 Unauthorized when no active session exists.
        /// </returns>

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]

        public async Task<IActionResult> Logout(
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 3.1: LOAD THE EXISTING SESSION
               ==================================================== */

            // Load the session information stored on the server.
            await HttpContext.Session.LoadAsync(
                cancellationToken);


            /* ====================================================
               SECTION 3.2: CHECK IF USER IS LOGGED IN
               ==================================================== */

            // Retrieve the UserId stored during login.
            int? userId = HttpContext.Session.GetInt32(
                "UserId");

            // Retrieve the user's assigned role.
            string? role = HttpContext.Session.GetString(
                "Role");

            // Reject the request if no valid login
            // information is stored in the session.
            if (userId == null ||
                userId <= 0 ||
                (role != "Organiser" && role != "Participant"))
            {
                return Unauthorized(new
                {
                    message = "You must be logged in to log out."
                });
            }


            /* ====================================================
               SECTION 3.3: CLEAR SERVER-SIDE SESSION
               ==================================================== */

            // Remove UserId, Role and other session values.
            HttpContext.Session.Clear();

            // Save the cleared session state.
            await HttpContext.Session.CommitAsync(
                cancellationToken);


            /* ====================================================
               SECTION 3.4: DELETE SESSION COOKIE
               ==================================================== */

            // Expire the RaceDay session cookie in the browser.
            // These options match the settings in Program.cs.
            Response.Cookies.Delete(
                ".RaceDay.Session",
                new CookieOptions
                {
                    Path = "/",

                    HttpOnly = true,

                    Secure = true,

                    SameSite = SameSiteMode.Lax
                });


            /* ====================================================
               SECTION 3.5: RETURN HTTP 204 NO CONTENT
               ==================================================== */

            // Confirm successful logout without a response body.
            return NoContent();
        }
    }
}
