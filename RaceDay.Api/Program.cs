using System;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api
{
    /* ============================================================
       Code Attribution

       TITLE: Authentication and Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for configuring authentication,
       authorization middleware and access policies.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for creating Organiser-only
       and Participant-only authorization policies.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Session and State Management in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for configuring server-side
       session storage and secure session cookies.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: DbContext Configuration
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for connecting the RaceDay API
       to SQL Server through Entity Framework Core.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Get Started with Swashbuckle and ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
       DATE ACCESSED: 18 October 2026

       Usage: Referenced for generating Swagger documentation
       and displaying API endpoints in the browser.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Quickstart: Azure Blob Storage client library for .NET
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for registering the blob storage service
       used to upload event banners and profile pictures.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Applying Migrations
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for applying migrations at application
       startup so a new Docker database gets its tables and roles.

       ============================================================ */

    // Configures the RaceDay RESTful API.
    public class Program
    {
        public static void Main(string[] args)
        {
            /* ====================================================
               SECTION 1: CREATE APPLICATION BUILDER
               ==================================================== */

            // Create the ASP.NET Core application builder.
            var builder = WebApplication.CreateBuilder(args);


            /* ====================================================
               SECTION 2: CONFIGURE SQL SERVER DATABASE
               ==================================================== */

            // Get the database connection string.
            var connectionString = builder.Configuration
                .GetConnectionString("RaceDayDatabase")
                ?? throw new InvalidOperationException(
                    "The RaceDayDatabase connection string was not found.");

            // Connect RaceDayDbContext to SQL Server.
            builder.Services.AddDbContext<RaceDayDbContext>(
                options =>
                    options.UseSqlServer(connectionString));


            /* ====================================================
               SECTION 3: REGISTER SERVICES
               ==================================================== */

            // Register the ASP.NET Core Identity password hasher.
            builder.Services.AddScoped<
                IPasswordHasher<User>,
                PasswordHasher<User>>();

            // Register the custom RaceDay password service.
            builder.Services.AddScoped<
                IPasswordService,
                PasswordService>();

            // Register the Azure Blob Storage image service.
            builder.Services.AddSingleton<
                IBlobStorageService,
                BlobStorageService>();


            /* ====================================================
               SECTION 4: CONFIGURE SERVER-SIDE SESSIONS
               ==================================================== */

            // Store session information in server-side memory.
            builder.Services.AddDistributedMemoryCache();

            // Configure the session cookie and timeout.
            builder.Services.AddSession(options =>
            {
                // Name used to identify the session cookie.
                options.Cookie.Name = ".RaceDay.Session";

                // Block JavaScript access to this cookie.
                options.Cookie.HttpOnly = true;

                // Only transmit the cookie over HTTPS.
                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;

                // Restrict cross-site cookie sending.
                options.Cookie.SameSite =
                    SameSiteMode.Lax;

                // Mark the session cookie as essential.
                options.Cookie.IsEssential = true;

                // Expire session data after 30 minutes of inactivity.
                options.IdleTimeout =
                    TimeSpan.FromMinutes(30);
            });


            /* ====================================================
               SECTION 5: CONFIGURE AUTHENTICATION
               ==================================================== */

            // Use the RaceDay session as our authentication scheme.
            builder.Services
                .AddAuthentication(
                    RaceDaySessionAuthenticationHandler.SchemeName)
                .AddScheme<
                    AuthenticationSchemeOptions,
                    RaceDaySessionAuthenticationHandler>(
                    RaceDaySessionAuthenticationHandler.SchemeName,
                    options => { });


            /* ====================================================
               SECTION 6: CONFIGURE ROLE-BASED AUTHORIZATION
               ==================================================== */

            // Define the roles allowed to access protected features.
            builder.Services.AddAuthorization(options =>
            {
                // Only authenticated Organisers can use this policy.
                options.AddPolicy(
                    "OrganiserOnly",
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole("Organiser");
                    });

                // Only authenticated Participants can use this policy.
                options.AddPolicy(
                    "ParticipantOnly",
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole("Participant");
                    });
            });


            /* ====================================================
               SECTION 7: REGISTER API CONTROLLERS
               ==================================================== */

            // Enable controller-based Web API endpoints.
            builder.Services.AddControllers();


            /* ====================================================
               SECTION 8: CONFIGURE OPENAPI AND SWAGGER
               ==================================================== */

            // Enable built-in ASP.NET Core OpenAPI support.
            builder.Services.AddOpenApi();

            // Include minimal API endpoints in Swagger.
            builder.Services.AddEndpointsApiExplorer();

            // Generate Swagger documentation.
            builder.Services.AddSwaggerGen();


            /* ====================================================
               SECTION 9: BUILD THE APPLICATION
               ==================================================== */

            // Build the configured RaceDay application.
            var app = builder.Build();


            /* ====================================================
               SECTION 9A: APPLY DATABASE MIGRATIONS (OPTIONAL)
               ==================================================== */

            // Off by default, so local runs and tests are unchanged.
            // Docker sets Database__MigrateOnStartup=true so an empty
            // SQL Server container gets its tables and seeded roles.
            if (app.Configuration.GetValue<bool>(
                    "Database:MigrateOnStartup"))
            {
                ApplyMigrations(app);
            }


            /* ====================================================
               SECTION 10: CONFIGURE DEVELOPMENT SWAGGER
               ==================================================== */

            if (app.Environment.IsDevelopment())
            {
                // Enable built-in OpenAPI.
                app.MapOpenApi();

                // Enable Swagger documentation.
                app.UseSwagger();

                // Enable interactive Swagger UI.
                app.UseSwaggerUI();
            }


            /* ====================================================
               SECTION 11: CONFIGURE REQUEST MIDDLEWARE
               ==================================================== */

            // Redirect HTTP requests to HTTPS. In Docker only plain
            // HTTP is bound, so no HTTPS port exists and the
            // middleware leaves requests alone.
            app.UseHttpsRedirection();

            // Match incoming requests to routes.
            app.UseRouting();

            // Make session state available to authentication.
            app.UseSession();

            // Identify the current user from the RaceDay session.
            app.UseAuthentication();

            // Enforce authentication and role permissions.
            app.UseAuthorization();


            /* ====================================================
               SECTION 12: MAP CONTROLLER ENDPOINTS
               ==================================================== */

            // Make all API controllers accessible.
            app.MapControllers();


            /* ====================================================
               SECTION 13: DEVELOPMENT-ONLY ROLE CHECKS
               ==================================================== */

            // Temporary endpoints for testing role permissions.
            // They are only available in Development.
            if (app.Environment.IsDevelopment())
            {
                // Only Organisers can access this test endpoint.
                app.MapGet(
                    "/api/dev/access/organiser",
                    () => Results.Ok(new
                    {
                        message = "Organiser access granted."
                    }))
                    .RequireAuthorization("OrganiserOnly")
                    .WithTags("Development Access Checks");

                // Only Participants can access this test endpoint.
                app.MapGet(
                    "/api/dev/access/participant",
                    () => Results.Ok(new
                    {
                        message = "Participant access granted."
                    }))
                    .RequireAuthorization("ParticipantOnly")
                    .WithTags("Development Access Checks");
            }


            /* ====================================================
               SECTION 14: START RACEDAY
               ==================================================== */

            // Start the ASP.NET Core API.
            app.Run();
        }

        /* ========================================================
           SECTION 15: MIGRATION HELPER
           ======================================================== */

        // Applies pending migrations, retrying while SQL Server
        // is still starting inside its container.
        private static void ApplyMigrations(WebApplication app)
        {
            using IServiceScope scope = app.Services.CreateScope();

            RaceDayDbContext database = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            ILogger<Program> logger = scope.ServiceProvider
                .GetRequiredService<ILogger<Program>>();

            const int maxAttempts = 10;

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    database.Database.Migrate();
                    logger.LogInformation(
                        "Database migrations applied.");
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(
                        ex,
                        "Database not ready (attempt {Attempt} of {Max}). Retrying in 3 seconds.",
                        attempt,
                        maxAttempts);

                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}