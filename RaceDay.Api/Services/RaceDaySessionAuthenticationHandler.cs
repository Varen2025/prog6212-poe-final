
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services
{
    /* ============================================================
       Code Attribution

       TITLE: AuthenticationHandler<TOptions> Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for implementing a custom ASP.NET
       Core authentication scheme for RaceDay sessions.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Session and State Management in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for reading the authenticated
       user's ID and role from server-side session state.

       ============================================================ */

    // Converts the existing RaceDay session into an
    // authenticated identity for ASP.NET Core authorization.
    public class RaceDaySessionAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        // Name used when registering the authentication scheme.
        public const string SchemeName = "RaceDaySession";

        // Database context used to verify the account and role.
        private readonly RaceDayDbContext _context;

        // Receive the ASP.NET Core authentication dependencies.
        public RaceDaySessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            RaceDayDbContext context)
            : base(options, logger, encoder)
        {
            _context = context;
        }

        /* ========================================================
           SECTION 1: AUTHENTICATE THE CURRENT REQUEST
           ======================================================== */

        protected override async Task<AuthenticateResult>
            HandleAuthenticateAsync()
        {
            // SECTION 1.1: LOAD THE SESSION

            // Retrieve the session values from server-side storage.
            await Context.Session.LoadAsync(
                Context.RequestAborted);

            // Read the user ID saved when login succeeded.
            int? userId = Context.Session.GetInt32(
                "UserId");

            // Read the role saved during login.
            string? sessionRole = Context.Session.GetString(
                "Role");


            // SECTION 1.2: CHECK FOR AN ACTIVE SESSION

            // A missing ID or role means the user is not logged in.
            if (userId == null ||
                userId <= 0 ||
                string.IsNullOrWhiteSpace(sessionRole))
            {
                return AuthenticateResult.NoResult();
            }


            // SECTION 1.3: VERIFY THE USER IN SQL SERVER

            // Retrieve the account and its actual database role.
            User? account = await _context.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .FirstOrDefaultAsync(
                    user => user.UserId == userId.Value,
                    Context.RequestAborted);

            // Reject a session for an account that no longer exists.
            if (account == null || account.Role == null)
            {
                return AuthenticateResult.Fail(
                    "The user account is no longer available.");
            }


            // SECTION 1.4: VERIFY THE REGISTERED ROLE

            // Compare the saved session role with the current
            // database role instead of trusting the session alone.
            string databaseRole = account.Role.RoleName;

            if (databaseRole != sessionRole ||
                (databaseRole != "Organiser" &&
                 databaseRole != "Participant"))
            {
                return AuthenticateResult.Fail(
                    "The session role is invalid.");
            }


            // SECTION 1.5: CREATE IDENTITY CLAIMS

            // Claims describe who the authenticated user is.
            // These values come from the verified database record.
            List<Claim> claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    account.UserId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    account.Email),

                new Claim(
                    ClaimTypes.Email,
                    account.Email),

                new Claim(
                    ClaimTypes.Role,
                    databaseRole)
            };


            // SECTION 1.6: CREATE THE AUTHENTICATED IDENTITY

            // Identify the request using our RaceDay scheme.
            ClaimsIdentity identity = new ClaimsIdentity(
                claims,
                SchemeName);

            ClaimsPrincipal principal = new ClaimsPrincipal(
                identity);


            // SECTION 1.7: RETURN SUCCESSFUL AUTHENTICATION

            // Give ASP.NET Core authorization the verified user.
            AuthenticationTicket ticket =
                new AuthenticationTicket(
                    principal,
                    SchemeName);

            return AuthenticateResult.Success(ticket);
        }
    }
}
