
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2
       PARTICIPANT ENROLMENT INTEGRATION TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Integration Tests in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for testing enrolment endpoints
       through real HTTP requests.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Async Queries in EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for verifying records saved
       to the isolated integration test database.

       ============================================================ */

    // Tests the Participant and Organiser enrolment operations.
    [TestClass]
    public class ParticipantEnrolmentTests
    {
        /* ========================================================
           SECTION 1: CREATE A PARTICIPANT TEST CLIENT
           ======================================================== */

        // Return a client authenticated as the given Participant.
        private static HttpClient CreateParticipantClient(
            RaceDayTestFactory factory,
            int participantId = RaceDayTestFactory.ParticipantId)
        {
            return factory.CreateTestClient(
                participantId,
                "Participant");
        }


        /* ========================================================
           SECTION 2: CREATE AN ORGANISER TEST CLIENT
           ======================================================== */

        private static HttpClient CreateOrganiserClient(
            RaceDayTestFactory factory,
            int organiserId = RaceDayTestFactory.OrganiserId)
        {
            return factory.CreateTestClient(
                organiserId,
                "Organiser");
        }


        /* ========================================================
           TEST 1: SUCCESSFUL PARTICIPANT ENROLMENT
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_ValidRequest_Returns201()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            var request = new
            {
                eventId = RaceDayTestFactory.EventId,
                categoryId = RaceDayTestFactory.AgeCategoryId
            };

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                request);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Created,
                response.StatusCode);

            // Confirm the new enrolment starts as Pending.
            var result = await response.Content
                .ReadFromJsonAsync<EnrolmentResponseDto>();

            Assert.IsNotNull(result);

            Assert.IsTrue(result.EnrolmentId > 0);

            Assert.AreEqual(
                RaceDayTestFactory.ParticipantId,
                result.ParticipantId);

            Assert.AreEqual("Pending", result.Status);
        }


        /* ========================================================
           TEST 2: VERIFY DATABASE PERSISTENCE
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_ShouldSaveToDatabase()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Created,
                response.StatusCode);

            // Open a separate database context.
            using var scope = factory.CreateDatabaseScope();

            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            // Confirm the enrolment exists in the database.
            var savedEnrolment = await context.Enrolments
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(
                RaceDayTestFactory.ParticipantId,
                savedEnrolment.ParticipantId);

            Assert.AreEqual(
                RaceDayTestFactory.EventId,
                savedEnrolment.EventId);

            Assert.AreEqual(
                RaceDayTestFactory.AgeCategoryId,
                savedEnrolment.CategoryId);

            Assert.AreEqual(
                "Pending",
                savedEnrolment.Status);
        }


        /* ========================================================
           TEST 3: PREVENT DUPLICATE ENROLMENTS
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_DuplicateEvent_Returns409()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Register for the event using an age category.
            var firstResponse = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            Assert.AreEqual(
                HttpStatusCode.Created,
                firstResponse.StatusCode);

            // Act
            // Attempt to enter the same event again,
            // this time using a different category.
            var secondResponse = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.DistanceCategoryId
                });

            // Assert
            // Only one enrolment per Participant per event.
            Assert.AreEqual(
                HttpStatusCode.Conflict,
                secondResponse.StatusCode);
        }


        /* ========================================================
           TEST 4: INVALID EVENT/CATEGORY COMBINATION
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_WrongEventCategory_Returns400()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Act
            // This category belongs to OtherEventId,
            // not the event supplied in the request.
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId =
                        RaceDayTestFactory.OtherEventCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }


        /* ========================================================
           TEST 5: INELIGIBLE AGE CATEGORY
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_IneligibleAge_Returns400()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // The Participant is in their twenties.
            // This category requires an age of 40 to 55.
            var request = new
            {
                eventId = RaceDayTestFactory.EventId,
                categoryId = RaceDayTestFactory.IneligibleCategoryId
            };

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                request);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }


        /* ========================================================
           TEST 6: VALID DISTANCE CATEGORY
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_DistanceCategory_Returns201()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.DistanceCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Created,
                response.StatusCode);
        }


        /* ========================================================
           TEST 7: NONEXISTENT EVENT
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_UnknownEvent_Returns404()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = 9999,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }


        /* ========================================================
           TEST 8: NONEXISTENT CATEGORY
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_UnknownCategory_Returns404()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = CreateParticipantClient(factory);

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = 9999
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }


        /* ========================================================
           TEST 9: PARTICIPANT VIEWS PERSONAL ENROLMENTS
           ======================================================== */

        [TestMethod]
        public async Task GetMyEnrolments_ReturnsOnlyMyEntries()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var firstClient =
                CreateParticipantClient(factory);

            using var secondClient =
                CreateParticipantClient(
                    factory,
                    RaceDayTestFactory.OtherParticipantId);

            // Create an enrolment for the first Participant.
            var firstResponse = await firstClient.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            Assert.AreEqual(
                HttpStatusCode.Created,
                firstResponse.StatusCode);

            // Create a separate enrolment for another Participant.
            var secondResponse = await secondClient.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            Assert.AreEqual(
                HttpStatusCode.Created,
                secondResponse.StatusCode);

            // Act
            var response = await firstClient.GetAsync(
                "/api/enrolments/me");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            var entries = await response.Content.ReadFromJsonAsync<
                List<EnrolmentResponseDto>>();

            Assert.IsNotNull(entries);

            // Only the first Participant's entry is returned.
            Assert.AreEqual(1, entries.Count);

            Assert.AreEqual(
                RaceDayTestFactory.ParticipantId,
                entries[0].ParticipantId);
        }


        /* ========================================================
           TEST 10: ORGANISER VIEWING THEIR OWN ENROLMENTS
           ======================================================== */

        [TestMethod]
        public async Task GetEventEnrolments_Owner_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var participant =
                CreateParticipantClient(factory);

            using var organiser =
                CreateOrganiserClient(factory);

            // Create a valid enrolment.
            var created = await participant.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            Assert.AreEqual(
                HttpStatusCode.Created,
                created.StatusCode);

            // Act
            var response = await organiser.GetAsync(
                $"/api/events/{RaceDayTestFactory.EventId}/enrolments");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            var entries = await response.Content.ReadFromJsonAsync<
                List<EnrolmentResponseDto>>();

            Assert.IsNotNull(entries);
            Assert.AreEqual(1, entries.Count);
        }


        /* ========================================================
           TEST 11: PREVENT ANOTHER ORGANISER FROM VIEWING
           ======================================================== */

        [TestMethod]
        public async Task GetEventEnrolments_NonOwner_Returns403()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            // Organiser 102 does not own Event 501.
            using var organiser = CreateOrganiserClient(
                factory,
                RaceDayTestFactory.OtherOrganiserId);

            // Act
            var response = await organiser.GetAsync(
                $"/api/events/{RaceDayTestFactory.EventId}/enrolments");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }


        /* ========================================================
           TEST 12: ORGANISER UPDATES ENROLMENT STATUS
           ======================================================== */

        [TestMethod]
        public async Task UpdateEnrolmentStatus_Owner_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var participant =
                CreateParticipantClient(factory);

            using var organiser =
                CreateOrganiserClient(factory);

            // Create the enrolment.
            var created = await participant.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            Assert.AreEqual(
                HttpStatusCode.Created,
                created.StatusCode);

            var entry = await created.Content.ReadFromJsonAsync<
                EnrolmentResponseDto>();

            Assert.IsNotNull(entry);

            // Act
            // Confirm the entry as the event's Organiser.
            var response = await organiser.PutAsJsonAsync(
                $"/api/enrolments/{entry.EnrolmentId}/status",
                new
                {
                    status = "Confirmed"
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);

            // Verify the database status changed.
            using var scope = factory.CreateDatabaseScope();

            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            var saved = await context.Enrolments
                .AsNoTracking()
                .SingleAsync(item =>
                    item.EnrolmentId == entry.EnrolmentId);

            Assert.AreEqual("Confirmed", saved.Status);
        }


        /* ========================================================
           TEST 13: ORGANISER CANNOT ENROL AS A PARTICIPANT
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_Organiser_Returns403()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var organiser =
                CreateOrganiserClient(factory);

            // Act
            var response = await organiser.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }


        /* ========================================================
           TEST 14: ANONYMOUS USER CANNOT ENROL
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_AnonymousUser_Returns401()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }


        /* ========================================================
           TEST 15: ENROLMENTS CLOSE AFTER EVENT START
           ======================================================== */

        [TestMethod]
        public async Task CreateEnrolment_PastEvent_Returns400()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = CreateParticipantClient(factory);

            // Change the fictional test event into a past event.
            using (var scope = factory.CreateDatabaseScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<RaceDayDbContext>();

                var raceEvent = await context.Events
                    .SingleAsync(item =>
                        item.EventId == RaceDayTestFactory.EventId);

                raceEvent.EventDate = DateTime.Now.AddDays(-7);

                await context.SaveChangesAsync();
            }

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/enrolments",
                new
                {
                    eventId = RaceDayTestFactory.EventId,
                    categoryId = RaceDayTestFactory.AgeCategoryId
                });

            // Assert
            Assert.AreEqual(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }
    }
}
