/*
   TITLE: Upload files in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for checking the size, file type and file
   signature of an uploaded image before it is sent to the
   RaceDay API.
*/

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services
{
    // Friendly checks done in MVC so a bad image is rejected on the
    // form BEFORE the event is saved. The API repeats the same checks
    // on the file's real bytes, so the two always agree.
    public static class ImageFileRules
    {
        // Same limit as the API: 2 MB.
        public const long MaxBytes = 2 * 1024 * 1024;

        private const string TypeMessage =
            "Only real JPG, PNG or WebP images are allowed. " +
            "Other formats (for example GIF, BMP, AVIF or HEIC) " +
            "are not supported, even if the file was renamed.";

        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp" };

        // For an optional image. No file chosen is fine.
        public static string? ValidateOptional(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            return Check(file);
        }

        // For an image the user must supply.
        public static string? ValidateRequired(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image to upload.";
            }

            return Check(file);
        }

        private static string? Check(IFormFile file)
        {
            if (file.Length > MaxBytes)
            {
                return "The image must be 2 MB or smaller.";
            }

            string extension = Path.GetExtension(file.FileName);

            bool allowed = Array.Exists(
                AllowedExtensions,
                item => string.Equals(
                    item, extension, StringComparison.OrdinalIgnoreCase));

            if (!allowed)
            {
                return TypeMessage;
            }

            // Check the real file signature, exactly like the API does.
            if (!HasImageSignature(file))
            {
                return TypeMessage;
            }

            return null;
        }

        // JPEG, PNG or WebP, identified from the first bytes.
        private static bool HasImageSignature(IFormFile file)
        {
            byte[] header = new byte[12];
            int read = 0;

            using (Stream stream = file.OpenReadStream())
            {
                while (read < header.Length)
                {
                    int count = stream.Read(
                        header, read, header.Length - read);

                    if (count == 0)
                    {
                        break;
                    }

                    read += count;
                }
            }

            // JPEG: FF D8 FF
            if (read >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return true;
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 &&
                header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A &&
                header[6] == 0x1A && header[7] == 0x0A)
            {
                return true;
            }

            // WebP: "RIFF" + 4 size bytes + "WEBP"
            if (read >= 12 &&
                header[0] == 0x52 && header[1] == 0x49 &&
                header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 &&
                header[10] == 0x42 && header[11] == 0x50)
            {
                return true;
            }

            return false;
        }
    }
}