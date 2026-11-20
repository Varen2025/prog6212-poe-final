
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceCategory = RaceDay.Api.Models.Category;
using RaceEvent = RaceDay.Api.Models.Event;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2

       EVENT MANAGEMENT AUTOMATED TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Write Tests with MSTest
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for creating MSTest test methods
       and checking expected results with assertions.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Unit Test Controller Logic in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/testing
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for testing controller actions
       independently of the HTTP request pipeline.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: In-Memory Database Provider - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/providers/in-memory/
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for creating isolated databases
       for RaceDay controller testing.

       ============================================================ */

    // Tests the existing RaceDay event management controller.
    [TestClass]
    public class EventManagementTests
    {
        /* ========================================================
           SECTION 1: CREATE AN ISOLATED TEST DATABASE
           ======================================================== */

        // Give each test a separate temporary database.
        // This prevents one test from affecting another.
        private static RaceDayDbContext CreateTestDatabase()
        {
            // Generate a unique database name.
            string databaseName =
                "RaceDayEventTests_" + Guid.NewGuid();

            // Configure Entity Framework Core.
            var options =
                new DbContextOptionsBuilder<RaceDayDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;

            // Create the actual RaceDay database context.
            var context = new RaceDayDbContext(options);

            // Initialise the in-memory database and seed data.
            context.Database.EnsureCreated();

            return context;
        }


        /* ========================================================
           SECTION 2: CREATE A TEST CONTROLLER
           ======================================================== */

        // Create the real EventsController with a test identity.
        // The user ID represents the logged-in Organiser.
        private static EventsController CreateController(
            RaceDayDbContext context,
            int? organiserId)
        {
            // Store authentication claims for the test user.
            var claims = new List<Claim>();

            // Create an authenticated Organiser identity
            // only when a user ID has been provided.
            ClaimsIdentity identity;

            if (organiserId.HasValue)
            {
                claims.Add(new Claim(
                    ClaimTypes.NameIdentifier,
                    organiserId.Value.ToString()));

                claims.Add(new Claim(
                    ClaimTypes.Role,
                    "Organiser"));

                identity = new ClaimsIdentity(
                    claims,
                    "TestAuthentication");
            }
            else
            {
                // An empty identity represents an anonymous user.
                identity = new ClaimsIdentity();
            }

            // Create a principal containing the test identity.
            var principal = new ClaimsPrincipal(identity);

            // Create an HTTP context for the controller.
            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            // Create the real event controller.
            // A fake blob service stands in for Azure Blob Storage.
            var controller = new EventsController(
                context,
                new FakeBlobStorageService());

            // Attach the test HTTP context.
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }


        /* ========================================================
           SECTION 3: CREATE A VALID EVENT REQUEST
           ======================================================== */

        // Use consistent valid event data throughout the tests.
        private static EventRequestDto CreateValidRequest()
        {
            return new EventRequestDto
            {
                Name = "Durban Summer 10K",

                Description =
                    "A community running event in Durban.",

                EventDate = DateTime.Now.AddDays(30),

                Location = "Durban Beachfront",

                DistanceKm = 10.00m,

                EventType = "Run"
            };
        }


        /* ========================================================
           SECTION 4: ADD AN EVENT FOR TESTING
           ======================================================== */

        // Save an event directly in the test database.
        // This is used when testing GET, PUT and DELETE.
        private static async Task<RaceEvent> AddTestEventAsync(
            RaceDayDbContext context,
            int organiserId,
            DateTime? eventDate = null,
            string eventType = "Run",
            string location = "Durban Beachfront")
        {
            // Create a valid database event record.
            var raceEvent = new RaceEvent
            {
                OrganiserId = organiserId,

                Name = "Durban Summer 10K",

                Description =
                    "A community running event in Durban.",

                EventDate =
                    eventDate ?? DateTime.Now.AddDays(30),

                Location = location,

                DistanceKm = 10.00m,

                EventType = eventType,

                BannerImageUrl = null,

                CreatedAt = DateTime.Now
            };

            // Save the event.
            context.Events.Add(raceEvent);

            await context.SaveChangesAsync();

            return raceEvent;
        }


        /* ========================================================
           SECTION 5: READ EVENT LIST RESPONSES
           ======================================================== */

        // Convert a successful controller response into
        // a list that our tests can inspect.
        private static List<EventResponseDto> ReadEventList(
            IActionResult response)
        {
            // Confirm the controller returned an HTTP 200 result.
            var okResult = response as OkObjectResult
                ?? throw new AssertFailedException(
                    "Expected an OkObjectResult.");

            // Confirm the returned value is a list of events.
            return okResult.Value as List<EventResponseDto>
                ?? throw new AssertFailedException(
                    "Expected a list of EventResponseDto objects.");
        }


        /* ========================================================
           SECTION 6: TEST SUCCESSFUL EVENT CREATION
           ======================================================== */

        // TEST 1: An authenticated Organiser creates an event.
        [TestMethod]
        public async Task CreateEvent_ValidRequest_ShouldReturn201()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, 101);

            EventRequestDto request = CreateValidRequest();

            // Act
            IActionResult response = await controller.CreateEvent(
                request,
                CancellationToken.None);

            // Assert
            var created = response as CreatedAtActionResult
                ?? throw new AssertFailedException(
                    "Expected HTTP 201 Created.");

            Assert.AreEqual(201, created.StatusCode);

            // Check the returned event details.
            var eventResponse = created.Value as EventResponseDto
                ?? throw new AssertFailedException(
                    "Expected EventResponseDto.");

            Assert.IsTrue(eventResponse.EventId > 0);

            Assert.AreEqual(101, eventResponse.OrganiserId);

            Assert.AreEqual(
                "Durban Summer 10K",
                eventResponse.Name);

            // Confirm the event was saved to the database.
            var savedEvent = await context.Events
                .SingleAsync();

            Assert.AreEqual(
                101,
                savedEvent.OrganiserId);
        }


        /* ========================================================
           SECTION 7: TEST EVENT CREATION WITHOUT IDENTITY
           ======================================================== */

        // TEST 2: Missing user identity must reject creation.
        [TestMethod]
        public async Task CreateEvent_NoUserId_ShouldReturn401()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, null);

            // Act
            IActionResult response = await controller.CreateEvent(
                CreateValidRequest(),
                CancellationToken.None);

            // Assert
            Assert.IsTrue(
                response is UnauthorizedResult);

            // No event should have been added.
            Assert.AreEqual(
                0,
                await context.Events.CountAsync());
        }


        /* ========================================================
           SECTION 8: TEST PUBLIC UPCOMING EVENTS
           ======================================================== */

        // TEST 3: Public listing should exclude past events.
        [TestMethod]
        public async Task GetEvents_ShouldReturnUpcomingEventsOnly()
        {
            // Arrange
            using var context = CreateTestDatabase();

            // Save a future event.
            await AddTestEventAsync(
                context,
                101,
                DateTime.Now.AddDays(20));

            // Save an event whose date has already passed.
            await AddTestEventAsync(
                context,
                101,
                DateTime.Now.AddDays(-20));

            EventsController controller =
                CreateController(context, null);

            // Act
            IActionResult response = await controller.GetEvents(
                null,
                null,
                null,
                CancellationToken.None);

            // Assert
            List<EventResponseDto> events =
                ReadEventList(response);

            Assert.AreEqual(1, events.Count);

            Assert.IsTrue(
                events[0].EventDate > DateTime.Now);
        }


        /* ========================================================
           SECTION 9: TEST INVALID EVENT TYPE FILTER
           ======================================================== */

        // TEST 4: Unsupported event types should be rejected.
        [TestMethod]
        public async Task GetEvents_InvalidType_ShouldReturn400()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, null);

            // Act
            IActionResult response = await controller.GetEvents(
                "Swim",
                null,
                null,
                CancellationToken.None);

            // Assert
            var result = response as BadRequestObjectResult;

            Assert.IsNotNull(result);

            Assert.AreEqual(400, result.StatusCode);
        }


        /* ========================================================
           SECTION 10: TEST EVENT FILTERING
           ======================================================== */

        // TEST 5: Event type, location and date must match.
        [TestMethod]
        public async Task GetEvents_WithFilters_ShouldReturnMatchingEvent()
        {
            // Arrange
            using var context = CreateTestDatabase();

            DateTime selectedDate =
                DateTime.Now.AddDays(10);

            // Create a matching running event in Durban.
            await AddTestEventAsync(
                context,
                101,
                selectedDate,
                "Run",
                "Durban Beachfront");

            // Create a different event in Johannesburg.
            await AddTestEventAsync(
                context,
                101,
                selectedDate,
                "Walk",
                "Johannesburg");

            EventsController controller =
                CreateController(context, null);

            DateOnly filterDate =
                DateOnly.FromDateTime(selectedDate);

            // Act
            IActionResult response = await controller.GetEvents(
                "Run",
                "Durban",
                filterDate,
                CancellationToken.None);

            // Assert
            List<EventResponseDto> events =
                ReadEventList(response);

            Assert.AreEqual(1, events.Count);

            Assert.AreEqual(
                "Run",
                events[0].EventType);

            Assert.AreEqual(
                "Durban Beachfront",
                events[0].Location);
        }


        /* ========================================================
           SECTION 11: TEST RETRIEVING AN EVENT
           ======================================================== */

        // TEST 6: An existing event should be returned.
        [TestMethod]
        public async Task GetEventById_ExistingEvent_ShouldReturn200()
        {
            // Arrange
            using var context = CreateTestDatabase();

            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            EventsController controller =
                CreateController(context, null);

            // Act
            IActionResult response = await controller.GetEventById(
                savedEvent.EventId,
                CancellationToken.None);

            // Assert
            var result = response as OkObjectResult
                ?? throw new AssertFailedException(
                    "Expected HTTP 200 OK.");

            Assert.AreEqual(200, result.StatusCode);

            var returnedEvent =
                result.Value as EventResponseDto;

            Assert.IsNotNull(returnedEvent);

            Assert.AreEqual(
                savedEvent.EventId,
                returnedEvent.EventId);
        }


        /* ========================================================
           SECTION 12: TEST NONEXISTENT EVENT
           ======================================================== */

        // TEST 7: A missing event should return HTTP 404.
        [TestMethod]
        public async Task GetEventById_MissingEvent_ShouldReturn404()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, null);

            // Act
            IActionResult response = await controller.GetEventById(
                9999,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(
                response is NotFoundObjectResult);
        }


        /* ========================================================
           SECTION 13: TEST ORGANISER EVENT OWNERSHIP
           ======================================================== */

        // TEST 8: Organisers should only retrieve their events.
        [TestMethod]
        public async Task GetMyEvents_ShouldReturnOnlyOwnedEvents()
        {
            // Arrange
            using var context = CreateTestDatabase();

            // Organiser 101 creates one event.
            await AddTestEventAsync(context, 101);

            // Organiser 202 creates a different event.
            await AddTestEventAsync(context, 202);

            EventsController controller =
                CreateController(context, 101);

            // Act
            IActionResult response = await controller.GetMyEvents(
                CancellationToken.None);

            // Assert
            List<EventResponseDto> events =
                ReadEventList(response);

            Assert.AreEqual(1, events.Count);

            Assert.AreEqual(
                101,
                events[0].OrganiserId);
        }


        /* ========================================================
           SECTION 14: TEST UPDATING AN OWNED EVENT
           ======================================================== */

        // TEST 9: The owner can update their event details.
        [TestMethod]
        public async Task UpdateEvent_Owner_ShouldReturn200()
        {
            // Arrange
            using var context = CreateTestDatabase();

            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            EventsController controller =
                CreateController(context, 101);

            EventRequestDto request = CreateValidRequest();

            // Change the name and location.
            request.Name = "Updated Durban 10K";

            request.Location = "Durban North";

            // Act
            IActionResult response = await controller.UpdateEvent(
                savedEvent.EventId,
                request,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(response is OkObjectResult);

            // Reload the database record.
            await context.Entry(savedEvent).ReloadAsync();

            Assert.AreEqual(
                "Updated Durban 10K",
                savedEvent.Name);

            Assert.AreEqual(
                "Durban North",
                savedEvent.Location);

            // Ownership must remain unchanged.
            Assert.AreEqual(
                101,
                savedEvent.OrganiserId);
        }


        /* ========================================================
           SECTION 15: PREVENT UNAUTHORISED EVENT UPDATES
           ======================================================== */

        // TEST 10: Another Organiser cannot update this event.
        [TestMethod]
        public async Task UpdateEvent_NonOwner_ShouldBeForbidden()
        {
            // Arrange
            using var context = CreateTestDatabase();

            // The event belongs to Organiser 101.
            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            // Organiser 202 attempts to edit it.
            EventsController controller =
                CreateController(context, 202);

            EventRequestDto request = CreateValidRequest();

            request.Name = "Unauthorised Change";

            // Act
            IActionResult response = await controller.UpdateEvent(
                savedEvent.EventId,
                request,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(
                response is ForbidResult);

            // Ensure the stored event was not modified.
            await context.Entry(savedEvent).ReloadAsync();

            Assert.AreEqual(
                "Durban Summer 10K",
                savedEvent.Name);
        }


        /* ========================================================
           SECTION 16: UPDATE A NONEXISTENT EVENT
           ======================================================== */

        // TEST 11: Updating a missing event must return 404.
        [TestMethod]
        public async Task UpdateEvent_MissingEvent_ShouldReturn404()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, 101);

            // Act
            IActionResult response = await controller.UpdateEvent(
                9999,
                CreateValidRequest(),
                CancellationToken.None);

            // Assert
            Assert.IsTrue(
                response is NotFoundObjectResult);
        }


        /* ========================================================
           SECTION 17: TEST DELETING AN OWNED EVENT
           ======================================================== */

        // TEST 12: The owner may delete an unused event.
        [TestMethod]
        public async Task DeleteEvent_Owner_ShouldReturn204()
        {
            // Arrange
            using var context = CreateTestDatabase();

            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            int eventId = savedEvent.EventId;

            EventsController controller =
                CreateController(context, 101);

            // Act
            IActionResult response = await controller.DeleteEvent(
                eventId,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(response is NoContentResult);

            // Confirm the record was deleted.
            bool stillExists = await context.Events
                .AnyAsync(item => item.EventId == eventId);

            Assert.IsFalse(stillExists);
        }


        /* ========================================================
           SECTION 18: PREVENT UNAUTHORISED DELETION
           ======================================================== */

        // TEST 13: Another Organiser cannot delete an event.
        [TestMethod]
        public async Task DeleteEvent_NonOwner_ShouldBeForbidden()
        {
            // Arrange
            using var context = CreateTestDatabase();

            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            EventsController controller =
                CreateController(context, 202);

            // Act
            IActionResult response = await controller.DeleteEvent(
                savedEvent.EventId,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(response is ForbidResult);

            // Verify the event remains in the database.
            bool stillExists = await context.Events
                .AnyAsync(
                    item => item.EventId == savedEvent.EventId);

            Assert.IsTrue(stillExists);
        }


        /* ========================================================
           SECTION 19: PREVENT DELETION WITH RELATED CATEGORIES
           ======================================================== */

        // TEST 14: An event with categories cannot be deleted.
        [TestMethod]
        public async Task DeleteEvent_WithCategory_ShouldReturn409()
        {
            // Arrange
            using var context = CreateTestDatabase();

            RaceEvent savedEvent =
                await AddTestEventAsync(context, 101);

            // Create a category belonging to this event.
            var category = new RaceCategory
            {
                EventId = savedEvent.EventId,

                CategoryName = "10K Category",

                CategoryType = "Distance",

                MinAge = null,

                MaxAge = null,

                DistanceKm = 10.00m
            };

            context.Categories.Add(category);

            await context.SaveChangesAsync();

            EventsController controller =
                CreateController(context, 101);

            // Act
            IActionResult response = await controller.DeleteEvent(
                savedEvent.EventId,
                CancellationToken.None);

            // Assert
            var result = response as ConflictObjectResult;

            Assert.IsNotNull(result);

            Assert.AreEqual(409, result.StatusCode);

            // Verify the event was not deleted.
            bool stillExists = await context.Events
                .AnyAsync(
                    item => item.EventId == savedEvent.EventId);

            Assert.IsTrue(stillExists);
        }


        /* ========================================================
           SECTION 20: DELETE A NONEXISTENT EVENT
           ======================================================== */

        // TEST 15: Deleting an unknown event must return 404.
        [TestMethod]
        public async Task DeleteEvent_MissingEvent_ShouldReturn404()
        {
            // Arrange
            using var context = CreateTestDatabase();

            EventsController controller =
                CreateController(context, 101);

            // Act
            IActionResult response = await controller.DeleteEvent(
                9999,
                CancellationToken.None);

            // Assert
            Assert.IsTrue(
                response is NotFoundObjectResult);
        }


        /* ========================================================
           SECTION 21: VERIFY ORGANISER POLICY ATTRIBUTES
           ======================================================== */

        // TEST 16: Management actions must declare the
        // OrganiserOnly authorization policy.
        [TestMethod]
        public void ManagementActions_ShouldRequireOrganiserPolicy()
        {
            // Arrange
            string[] protectedActions =
            {
                nameof(EventsController.GetMyEvents),

                nameof(EventsController.CreateEvent),

                nameof(EventsController.UpdateEvent),

                nameof(EventsController.DeleteEvent)
            };

            // Act and Assert
            foreach (string actionName in protectedActions)
            {
                // Locate the action method by its name.
                MethodInfo? method = typeof(EventsController)
                    .GetMethod(actionName);

                Assert.IsNotNull(method);

                // Read its authorization attribute.
                AuthorizeAttribute? authorization =
                    method.GetCustomAttribute<AuthorizeAttribute>();

                Assert.IsNotNull(authorization);

                // Confirm the correct policy is configured.
                Assert.AreEqual(
                    "OrganiserOnly",
                    authorization.Policy);
            }
        }


        /* ========================================================
           SECTION 22: VERIFY PUBLIC EVENT ENDPOINTS
           ======================================================== */

        // TEST 17: Browsing and retrieving event details
        // should explicitly allow anonymous users.
        [TestMethod]
        public void PublicEventActions_ShouldAllowAnonymous()
        {
            // Arrange
            string[] publicActions =
            {
                nameof(EventsController.GetEvents),

                nameof(EventsController.GetEventById)
            };

            // Act and Assert
            foreach (string actionName in publicActions)
            {
                MethodInfo? method = typeof(EventsController)
                    .GetMethod(actionName);

                Assert.IsNotNull(method);

                // Verify [AllowAnonymous] is present.
                AllowAnonymousAttribute? attribute =
                    method.GetCustomAttribute<
                        AllowAnonymousAttribute>();

                Assert.IsNotNull(attribute);
            }
        }
    }
}