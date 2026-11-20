using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       RACEDAY EVENT MANAGEMENT SYSTEM
       PROG6212 - POE PART 3
       RESULT POSITION AND TOTAL FINISHERS INTEGRATION TESTS
       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Integration Tests in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for testing the result endpoints through
       real HTTP requests against an isolated test database.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Querying Data - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/querying/
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for counting related enrolments inside a
       query projection to calculate the total finishers.

       ============================================================ */

    // Tests that Participants see their position out of the total
    // finishers, and that unpublished results stay hidden.
    [TestClass]
    public class ResultTotalFinishersTests
    {
        // A third Participant, used for a Pending (non-finishing) entry.
        private const int ThirdParticipantId = 203;


        /* ========================================================
           SECTION 1: SEED A FINISHED RACE
           ======================================================== */

        // Turns the seeded event into a past race with two Completed
        // enrolments (each with a result) and one Pending enrolment.
        private static async Task SeedFinishedRaceAsync(
            RaceDayTestFactory factory,
            bool firstResultPublished)
        {
            using IServiceScope scope = factory.CreateDatabaseScope();

            RaceDayDbContext context = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            // The race has already been run.
            Event raceEvent = await context.Events.SingleAsync(item =>
                item.EventId == RaceDayTestFactory.EventId);

            raceEvent.EventDate = DateTime.Now.AddDays(-7);

            context.Users.Add(new User
            {
                UserId = ThirdParticipantId,
                RoleId = 2,
                FirstName = "Third",
                LastName = "Participant",
                Email = "participant203@example.com",
                PasswordHash = "TEST_ONLY_NOT_A_REAL_HASH",
                PhoneNumber = "0825550203",
                DateOfBirth = new DateOnly(1999, 3, 3),
                CreatedAt = DateTime.Now
            });

            await context.SaveChangesAsync();

            Enrolment first = new Enrolment
            {
                ParticipantId = RaceDayTestFactory.ParticipantId,
                EventId = RaceDayTestFactory.EventId,
                CategoryId = RaceDayTestFactory.DistanceCategoryId,
                EnrolmentDate = DateTime.Now.AddDays(-20),
                Status = "Completed"
            };

            Enrolment second = new Enrolment
            {
                ParticipantId = RaceDayTestFactory.OtherParticipantId,
                EventId = RaceDayTestFactory.EventId,
                CategoryId = RaceDayTestFactory.DistanceCategoryId,
                EnrolmentDate = DateTime.Now.AddDays(-20),
                Status = "Completed"
            };

            // This entry did not finish, so it must not be counted.
            Enrolment third = new Enrolment
            {
                ParticipantId = ThirdParticipantId,
                EventId = RaceDayTestFactory.EventId,
                CategoryId = RaceDayTestFactory.DistanceCategoryId,
                EnrolmentDate = DateTime.Now.AddDays(-20),
                Status = "Pending"
            };

            context.Enrolments.AddRange(first, second, third);

            await context.SaveChangesAsync();

            context.Results.AddRange(
                new Result
                {
                    EnrolmentId = first.EnrolmentId,
                    FinishTime = new TimeOnly(0, 52, 10),
                    FinishingPosition = 2,
                    IsPublished = firstResultPublished,
                    PublishedAt = firstResultPublished
                        ? DateTime.Now
                        : null
                },
                new Result
                {
                    EnrolmentId = second.EnrolmentId,
                    FinishTime = new TimeOnly(0, 48, 30),
                    FinishingPosition = 1,
                    IsPublished = true,
                    PublishedAt = DateTime.Now
                });

            await context.SaveChangesAsync();
        }


        /* ========================================================
           TEST 1: PARTICIPANT SEES POSITION AND TOTAL FINISHERS
           ======================================================== */

        [TestMethod]
        public async Task GetMyResults_ReturnsPositionAndTotalFinishers()
        {
            // Arrange
            using RaceDayTestFactory factory = new RaceDayTestFactory();

            using HttpClient client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId,
                "Participant");

            await SeedFinishedRaceAsync(factory, true);

            // Act
            HttpResponseMessage response =
                await client.GetAsync("/api/results/me");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            List<ResultResponseDto>? results =
                await response.Content.ReadFromJsonAsync<
                    List<ResultResponseDto>>();

            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(2, results[0].FinishingPosition);

            // Two Completed enrolments. The Pending one is not counted.
            Assert.AreEqual(2, results[0].TotalFinishers);
        }


        /* ========================================================
           TEST 2: UNPUBLISHED RESULTS ARE HIDDEN FROM PARTICIPANTS
           ======================================================== */

        [TestMethod]
        public async Task GetMyResults_UnpublishedResult_IsNotReturned()
        {
            // Arrange
            using RaceDayTestFactory factory = new RaceDayTestFactory();

            using HttpClient client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId,
                "Participant");

            await SeedFinishedRaceAsync(factory, false);

            // Act
            HttpResponseMessage response =
                await client.GetAsync("/api/results/me");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            List<ResultResponseDto>? results =
                await response.Content.ReadFromJsonAsync<
                    List<ResultResponseDto>>();

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }


        /* ========================================================
           TEST 3: ORGANISER SEES TOTAL FINISHERS ON EVENT RESULTS
           ======================================================== */

        [TestMethod]
        public async Task GetEventResults_Owner_ReturnsTotalFinishers()
        {
            // Arrange
            using RaceDayTestFactory factory = new RaceDayTestFactory();

            using HttpClient client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId,
                "Organiser");

            await SeedFinishedRaceAsync(factory, false);

            // Act
            HttpResponseMessage response = await client.GetAsync(
                $"/api/events/{RaceDayTestFactory.EventId}/results");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            List<ResultResponseDto>? results =
                await response.Content.ReadFromJsonAsync<
                    List<ResultResponseDto>>();

            Assert.IsNotNull(results);

            // Organisers see published and unpublished results.
            Assert.AreEqual(2, results.Count);

            foreach (ResultResponseDto result in results)
            {
                Assert.AreEqual(2, result.TotalFinishers);
            }
        }
    }
}