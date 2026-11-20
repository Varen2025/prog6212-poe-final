using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests
{
    /* ============================================================
       Code Attribution

       TITLE: Integration Tests in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for testing multipart image uploads
       against the real RaceDay routing and authorization.

       ============================================================ */

    [TestClass]
    public class ImageUploadTests
    {
        private static readonly byte[] PngHeader =
            { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Builds a fake image: real PNG signature + padding.
        private static byte[] MakePng(int totalBytes = 64)
        {
            byte[] bytes = new byte[totalBytes];
            Array.Copy(PngHeader, bytes, PngHeader.Length);
            return bytes;
        }

        private static MultipartFormDataContent BuildForm(
            byte[] bytes, string fileName, string contentType)
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType =
                new MediaTypeHeaderValue(contentType);
            form.Add(file, "file", fileName);
            return form;
        }

        private static string BannerUrl(int eventId) =>
            $"/api/events/{eventId}/banner";

        private const string ProfileUrl = "/api/users/me/profile-image";


        /* ===================== BANNER TESTS ===================== */

        [TestMethod]
        public async Task UploadBanner_OwnerValidPng_SavesUrl()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId, "Organiser");

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(MakePng(), "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.CreateDatabaseScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();
            var stored = await context.Events
                .FindAsync(RaceDayTestFactory.EventId);

            StringAssert.StartsWith(
                stored!.BannerImageUrl,
                FakeBlobStorageService.FakeHost);
        }

        [TestMethod]
        public async Task UploadBanner_OtherOrganiser_IsForbidden()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OtherOrganiserId, "Organiser");

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(MakePng(), "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_Participant_IsForbidden()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId, "Participant");

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(MakePng(), "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_Anonymous_IsUnauthorized()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(MakePng(), "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_TextFileNamedPng_IsRejected()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId, "Organiser");

            byte[] notAnImage =
                System.Text.Encoding.UTF8.GetBytes(
                    "This is plain text, not an image at all.");

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(notAnImage, "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_TooLarge_IsRejected()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId, "Organiser");

            // 3 MB, over the 2 MB limit.
            byte[] big = MakePng(3 * 1024 * 1024);

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                BuildForm(big, "big.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_NoFile_IsRejected()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId, "Organiser");

            var response = await client.PostAsync(
                BannerUrl(RaceDayTestFactory.EventId),
                new MultipartFormDataContent());

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UploadBanner_MissingEvent_IsNotFound()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.OrganiserId, "Organiser");

            var response = await client.PostAsync(
                BannerUrl(99999),
                BuildForm(MakePng(), "banner.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }


        /* ================= PROFILE IMAGE TESTS ================== */

        [TestMethod]
        public async Task UploadProfileImage_Participant_SavesUrl()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient(
                RaceDayTestFactory.ParticipantId, "Participant");

            var response = await client.PostAsync(
                ProfileUrl,
                BuildForm(MakePng(), "me.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.CreateDatabaseScope();
            var context = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();
            var stored = await context.Users
                .FindAsync(RaceDayTestFactory.ParticipantId);

            StringAssert.StartsWith(
                stored!.ProfileImageUrl,
                FakeBlobStorageService.FakeHost);
        }

        [TestMethod]
        public async Task UploadProfileImage_Anonymous_IsUnauthorized()
        {
            using var factory = new RaceDayTestFactory();
            using var client = factory.CreateTestClient();

            var response = await client.PostAsync(
                ProfileUrl,
                BuildForm(MakePng(), "me.png", "image/png"));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}