using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace RaceDay.Api.Services
{
    /* ============================================================
       Code Attribution

       TITLE: Upload a blob with .NET
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-upload
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for uploading image streams to
       Azure Blob Storage with a content type.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Safe storage of app secrets in development
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for reading the storage connection
       string from user secrets or environment variables.

       ============================================================ */

    // Stores RaceDay images in Azure Blob Storage.
    // The connection string comes from configuration only
    // (user-secrets locally, environment variables in Docker).
    public class BlobStorageService : IBlobStorageService
    {
        // Created on first use so the API can start (and tests
        // can run) even when no storage account is configured.
        private readonly Lazy<BlobContainerClient> _container;

        public BlobStorageService(IConfiguration configuration)
        {
            _container = new Lazy<BlobContainerClient>(() =>
            {
                string? connectionString =
                    configuration["BlobStorage:ConnectionString"];

                string? containerName =
                    configuration["BlobStorage:ContainerName"];

                if (string.IsNullOrWhiteSpace(connectionString) ||
                    string.IsNullOrWhiteSpace(containerName))
                {
                    throw new InvalidOperationException(
                        "Azure Blob Storage is not configured.");
                }

                return new BlobContainerClient(
                    connectionString,
                    containerName);
            });
        }

        public async Task<string> UploadImageAsync(
            Stream content,
            string contentType,
            string folder,
            string extension,
            CancellationToken cancellationToken)
        {
            // Random name: users cannot overwrite or guess files.
            string blobName =
                $"{folder}/{Guid.NewGuid():N}{extension}";

            try
            {
                BlobClient blob =
                    _container.Value.GetBlobClient(blobName);

                await blob.UploadAsync(
                    content,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    },
                    cancellationToken);

                return blob.Uri.ToString();
            }
            catch (RequestFailedException exception)
            {
                // Hide Azure details from API callers.
                throw new InvalidOperationException(
                    "Image storage is currently unavailable.",
                    exception);
            }
        }
    }
}