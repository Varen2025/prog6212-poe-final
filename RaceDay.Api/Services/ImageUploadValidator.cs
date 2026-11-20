using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services
{
    /* ============================================================
       Code Attribution

       TITLE: Upload files in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for validating uploaded files by size
       and file signature instead of trusting the file name.

       ============================================================ */

    // Result of checking an uploaded image.
    public sealed record ImageCheckResult(
        bool IsValid,
        string? Error,
        string ContentType,
        string Extension);

    // Shared rules for banner and profile image uploads.
    public static class ImageUploadValidator
    {
        // 2 MB keeps storage and page loads reasonable.
        public const long MaxBytes = 2 * 1024 * 1024;

        public static async Task<ImageCheckResult> ValidateAsync(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return Fail("Please choose an image file to upload.");
            }

            if (file.Length > MaxBytes)
            {
                return Fail("The image must be 2 MB or smaller.");
            }

            // Read the first bytes and identify the real format.
            byte[] header = new byte[12];

            await using var stream = file.OpenReadStream();

            int read = await stream.ReadAtLeastAsync(
                header,
                header.Length,
                throwOnEndOfStream: false,
                cancellationToken);

            if (read >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return new ImageCheckResult(
                    true, null, "image/jpeg", ".jpg");
            }

            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 &&
                header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A &&
                header[6] == 0x1A && header[7] == 0x0A)
            {
                return new ImageCheckResult(
                    true, null, "image/png", ".png");
            }

            // WebP: "RIFF" then 4 size bytes then "WEBP".
            if (read >= 12 &&
                header[0] == 0x52 && header[1] == 0x49 &&
                header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 &&
                header[10] == 0x42 && header[11] == 0x50)
            {
                return new ImageCheckResult(
                    true, null, "image/webp", ".webp");
            }

            return Fail("Only JPG, PNG or WebP images are allowed.");
        }

        private static ImageCheckResult Fail(string error)
        {
            return new ImageCheckResult(
                false, error, string.Empty, string.Empty);
        }
    }
}