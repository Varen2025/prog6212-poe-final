using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RaceDay.Api.Services
{
    /* ============================================================
       Code Attribution

       TITLE: Quickstart: Azure Blob Storage client library for .NET
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for designing the blob storage
       abstraction used by the RaceDay upload endpoints.

       ============================================================ */

    // Lets controllers store images without knowing about Azure.
    // Tests replace this with a fake so no Azure account is needed.
    public interface IBlobStorageService
    {
        // Uploads an image and returns its public URL.
        Task<string> UploadImageAsync(
            Stream content,
            string contentType,
            string folder,
            string extension,
            CancellationToken cancellationToken);
    }
}