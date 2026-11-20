
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2
       SHARED INTEGRATION TEST FACTORY
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Integration Tests in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for WebApplicationFactory,
       ConfigureTestServices and test authentication.

       ============================================================ */

    // Replaces external dependencies while preserving the
    // RaceDay application's real HTTP routing and middleware.
    public class RaceDayTestFactory :
        WebApplicationFactory<RaceDay.Api.Program>
    {
        // Fixed identifiers make our test scenarios easy to follow.
        public const int OrganiserId = 101;
        public const int OtherOrganiserId = 102;

        public const int ParticipantId = 201;
        public const int OtherParticipantId = 202;

        public const int EventId = 501;
        public const int OtherEventId = 502;

        public const int AgeCategoryId = 601;
        public const int IneligibleCategoryId = 602;
        public const int DistanceCategoryId = 603;
        public const int OtherEventCategoryId = 604;

        // Give each factory its own temporary database.
        private readonly string _databaseName =
            "RaceDayIntegration_" + Guid.NewGuid();

        // Prevent the same factory from being seeded twice.
        private readonly object _seedLock = new object();
        private bool _seeded;


        /* ========================================================
           SECTION 1: CONFIGURE THE TEST APPLICATION
           ======================================================== */

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            // Use Development for the existing API pipeline.
            builder.UseEnvironment("Development");

            // Satisfy the API connection-string configuration.
            // The real SQL Server provider is replaced below.
            builder.UseSetting(
                "ConnectionStrings:RaceDayDatabase",
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=RaceDayIntegrationUnused;" +
                "Trusted_Connection=True;");

            builder.ConfigureTestServices(services =>
            {
                // Remove the existing SQL Server configuration.
                services.RemoveAll<
                    DbContextOptions<RaceDayDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<RaceDayDbContext>>();

                // Register an isolated in-memory database.
                services.AddDbContext<RaceDayDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                // Never talk to Azure in tests.
                services.RemoveAll<IBlobStorageService>();
                services.AddSingleton<
                    IBlobStorageService,
                    FakeBlobStorageService>();

                // Replace authentication only in the test host.
                // Existing authorization policies remain active.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultForbidScheme =
                        TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        options => { });
            });
        }


        /* ========================================================
           SECTION 2: CREATE AN HTTP TEST CLIENT
           ======================================================== */

        // An omitted user ID represents an anonymous request.
        // Role is included only for authenticated test users.
        public HttpClient CreateTestClient(
            int? userId = null,
            string? role = null)
        {
            // HTTPS prevents development redirect interference.
            HttpClient client = CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            // Prepare known events, users and categories.
            SeedTestData();

            // Send test identity information in HTTP headers.
            if (userId.HasValue)
            {
                client.DefaultRequestHeaders.Add(
                    "X-Test-UserId",
                    userId.Value.ToString());

                client.DefaultRequestHeaders.Add(
                    "X-Test-Role",
                    role ?? "Participant");
            }

            return client;
        }


        /* ========================================================
           SECTION 3: CREATE TEST DATABASE RECORDS
           ======================================================== */

        private void SeedTestData()
        {
            lock (_seedLock)
            {
                if (_seeded)
                {
                    return;
                }

                // Resolve the test database from DI.
                using var scope = Services.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDayDbContext>();

                context.Database.EnsureCreated();

                // Create the two RaceDay roles if necessary.
                if (!context.Roles.Any(r => r.RoleId == 1))
                {
                    context.Roles.Add(new Role
                    {
                        RoleId = 1,
                        RoleName = "Organiser"
                    });
                }

                if (!context.Roles.Any(r => r.RoleId == 2))
                {
                    context.Roles.Add(new Role
                    {
                        RoleId = 2,
                        RoleName = "Participant"
                    });
                }

                context.SaveChanges();


                // Create two fictional Organiser accounts.
                context.Users.Add(new User
                {
                    UserId = OrganiserId,
                    RoleId = 1,
                    FirstName = "First",
                    LastName = "Organiser",
                    Email = "organiser101@example.com",
                    PasswordHash = "TEST_ONLY_NOT_A_REAL_HASH",
                    PhoneNumber = "0825550101",
                    DateOfBirth = new DateOnly(1995, 1, 1),
                    CreatedAt = DateTime.Now
                });

                context.Users.Add(new User
                {
                    UserId = OtherOrganiserId,
                    RoleId = 1,
                    FirstName = "Second",
                    LastName = "Organiser",
                    Email = "organiser102@example.com",
                    PasswordHash = "TEST_ONLY_NOT_A_REAL_HASH",
                    PhoneNumber = "0825550102",
                    DateOfBirth = new DateOnly(1995, 2, 2),
                    CreatedAt = DateTime.Now
                });


                // Create two fictional Participant accounts.
                context.Users.Add(new User
                {
                    UserId = ParticipantId,
                    RoleId = 2,
                    FirstName = "Test",
                    LastName = "Participant",
                    Email = "participant201@example.com",
                    PasswordHash = "TEST_ONLY_NOT_A_REAL_HASH",
                    PhoneNumber = "0825550201",
                    DateOfBirth = new DateOnly(2002, 6, 15),
                    CreatedAt = DateTime.Now
                });

                context.Users.Add(new User
                {
                    UserId = OtherParticipantId,
                    RoleId = 2,
                    FirstName = "Other",
                    LastName = "Participant",
                    Email = "participant202@example.com",
                    PasswordHash = "TEST_ONLY_NOT_A_REAL_HASH",
                    PhoneNumber = "0825550202",
                    DateOfBirth = new DateOnly(2000, 2, 10),
                    CreatedAt = DateTime.Now
                });

                context.SaveChanges();


                // Create an upcoming event owned by Organiser 101.
                context.Events.Add(new Event
                {
                    EventId = EventId,
                    OrganiserId = OrganiserId,
                    Name = "Durban Test 10K",
                    Description = "Automated enrolment testing event.",
                    EventDate = DateTime.Now.AddDays(30),
                    Location = "Durban Beachfront",
                    DistanceKm = 10.00m,
                    EventType = "Run",
                    CreatedAt = DateTime.Now
                });

                // Create another event owned by Organiser 102.
                context.Events.Add(new Event
                {
                    EventId = OtherEventId,
                    OrganiserId = OtherOrganiserId,
                    Name = "Umhlanga Test Walk",
                    Description = "Second Organiser testing event.",
                    EventDate = DateTime.Now.AddDays(40),
                    Location = "Umhlanga",
                    DistanceKm = 5.00m,
                    EventType = "Walk",
                    CreatedAt = DateTime.Now
                });

                context.SaveChanges();


                // Eligible age category for Participant 201.
                context.Categories.Add(new Category
                {
                    CategoryId = AgeCategoryId,
                    EventId = EventId,
                    CategoryName = "Adults 18-35",
                    CategoryType = "Age",
                    MinAge = 18,
                    MaxAge = 35,
                    DistanceKm = null
                });

                // Category that Participant 201 is too young for.
                context.Categories.Add(new Category
                {
                    CategoryId = IneligibleCategoryId,
                    EventId = EventId,
                    CategoryName = "Veterans 40-55",
                    CategoryType = "Age",
                    MinAge = 40,
                    MaxAge = 55,
                    DistanceKm = null
                });

                // Valid distance category.
                context.Categories.Add(new Category
                {
                    CategoryId = DistanceCategoryId,
                    EventId = EventId,
                    CategoryName = "10K Distance",
                    CategoryType = "Distance",
                    MinAge = null,
                    MaxAge = null,
                    DistanceKm = 10.00m
                });

                // Category belonging to the other event.
                context.Categories.Add(new Category
                {
                    CategoryId = OtherEventCategoryId,
                    EventId = OtherEventId,
                    CategoryName = "Umhlanga 5K",
                    CategoryType = "Distance",
                    MinAge = null,
                    MaxAge = null,
                    DistanceKm = 5.00m
                });

                context.SaveChanges();

                _seeded = true;
            }
        }


        /* ========================================================
           SECTION 4: READ DATABASE DATA DURING TESTS
           ======================================================== */

        // Obtain a fresh scope for checking stored records.
        public IServiceScope CreateDatabaseScope()
        {
            return Services.CreateScope();
        }
    }


    /* ============================================================
       SECTION 5: TEST-ONLY AUTHENTICATION HANDLER
       ============================================================ */

    // Never register this handler in the production API.
    // It is available only to the integration test host.
    public class TestAuthenticationHandler :
        AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "RaceDayTest";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult>
            HandleAuthenticateAsync()
        {
            // Missing header means the user is anonymous.
            if (!Request.Headers.TryGetValue(
                "X-Test-UserId",
                out var userIdHeader))
            {
                return Task.FromResult(
                    AuthenticateResult.NoResult());
            }

            // Reject invalid test identifiers.
            if (!int.TryParse(
                userIdHeader.ToString(),
                out int userId))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail(
                        "Invalid test user identifier."));
            }

            // Read the simulated user's role.
            string role =
                Request.Headers["X-Test-Role"].ToString();

            // Create the same claim types used by RaceDay.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new Claim(
                    ClaimTypes.Role,
                    role),

                new Claim(
                    ClaimTypes.Name,
                    "RaceDay Test User")
            };

            // Mark the identity as authenticated.
            var identity = new ClaimsIdentity(
                claims,
                SchemeName);

            var principal = new ClaimsPrincipal(identity);

            // Create a successful authentication ticket.
            var ticket = new AuthenticationTicket(
                principal,
                SchemeName);

            return Task.FromResult(
                AuthenticateResult.Success(ticket));
        }
    }
}