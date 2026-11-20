using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RaceDay.Api.Services;

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

       Usage: Referenced for replacing an external service
       with a fake implementation in the test host.

       ============================================================ */

    // Pretends to upload so tests never contact Azure.
    public class FakeBlobStorageService : IBlobStorageService
    {
        public const string FakeHost = "https://fake-blob.test/raceday-images/";

        public Task<string> UploadImageAsync(
            Stream content,
            string contentType,
            string folder,
            string extension,
            CancellationToken cancellationToken)
        {
            string url =
                $"{FakeHost}{folder}/{Guid.NewGuid():N}{extension}";

            return Task.FromResult(url);
        }
    }
}