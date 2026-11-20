
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 2
       HTTP AUTHORIZATION INTEGRATION TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Integration Tests in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for verifying authentication
       and authorization through real HTTP requests.

       ============================================================ */

    // Tests role restrictions using the RaceDay HTTP pipeline.
    [TestClass]
    public class AuthorizationTests
    {
        /* ========================================================
           TEST 1: ANONYMOUS DASHBOARD REQUEST
           ======================================================== */

        [TestMethod]
        public async Task Dashboard_AnonymousUser_Returns401()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            // Act
            var response = await client.GetAsync(
                "/api/organiser/dashboard");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }


        /* ========================================================
           TEST 2: PARTICIPANT DASHBOARD REQUEST
           ======================================================== */

        [TestMethod]
        public async Task Dashboard_Participant_Returns403()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId,
                "Participant");

            // Act
            var response = await client.GetAsync(
                "/api/organiser/dashboard");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }


        /* ========================================================
           TEST 3: ORGANISER DASHBOARD REQUEST
           ======================================================== */

        [TestMethod]
        public async Task Dashboard_Organiser_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId,
                "Organiser");

            // Act
            var response = await client.GetAsync(
                "/api/organiser/dashboard");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }


        /* ========================================================
           TEST 4: ANONYMOUS EVENT CREATION
           ======================================================== */

        [TestMethod]
        public async Task CreateEvent_AnonymousUser_Returns401()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            var request = new
            {
                name = "RaceDay Test Event",
                description = "Test event description.",
                eventDate = DateTime.Now.AddDays(60),
                location = "Durban",
                distanceKm = 10.00m,
                eventType = "Run"
            };

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/events",
                request);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }


        /* ========================================================
           TEST 5: PARTICIPANT EVENT CREATION
           ======================================================== */

        [TestMethod]
        public async Task CreateEvent_Participant_Returns403()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId,
                "Participant");

            var request = new
            {
                name = "Unauthorised Test Event",
                description = "Should not be created.",
                eventDate = DateTime.Now.AddDays(60),
                location = "Durban",
                distanceKm = 10.00m,
                eventType = "Run"
            };

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/events",
                request);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }


        /* ========================================================
           TEST 6: ORGANISER EVENT CREATION
           ======================================================== */

        [TestMethod]
        public async Task CreateEvent_Organiser_Returns201()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId,
                "Organiser");

            var request = new
            {
                name = "Durban Integration Run",
                description = "Event created through an HTTP test.",
                eventDate = DateTime.Now.AddDays(60),
                location = "Durban Beachfront",
                distanceKm = 10.00m,
                eventType = "Run"
            };

            // Act
            var response = await client.PostAsJsonAsync(
                "/api/events",
                request);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Created,
                response.StatusCode);
        }


        /* ========================================================
           TEST 7: PUBLIC EVENT BROWSING
           ======================================================== */

        [TestMethod]
        public async Task GetEvents_AnonymousUser_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            // Act
            var response = await client.GetAsync(
                "/api/events");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }


        /* ========================================================
           TEST 8: ORGANISER'S OWN EVENTS
           ======================================================== */

        [TestMethod]
        public async Task GetMyEvents_Organiser_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId,
                "Organiser");

            // Act
            var response = await client.GetAsync(
                "/api/events/mine");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }


        /* ========================================================
           TEST 9: PARTICIPANT'S PERSONAL ENROLMENT LIST
           ======================================================== */

        [TestMethod]
        public async Task MyEnrolments_Participant_Returns200()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId,
                "Participant");

            // Act
            var response = await client.GetAsync(
                "/api/enrolments/me");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                response.StatusCode);
        }


        /* ========================================================
           TEST 10: ORGANISER ACCESS TO PARTICIPANT ENDPOINT
           ======================================================== */

        [TestMethod]
        public async Task MyEnrolments_Organiser_Returns403()
        {
            // Arrange
            using var factory = new RaceDayTestFactory();

            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId,
                "Organiser");

            // Act
            var response = await client.GetAsync(
                "/api/enrolments/me");

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Forbidden,
                response.StatusCode);
        }
    }
}
