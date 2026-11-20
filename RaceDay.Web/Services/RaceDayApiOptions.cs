/*
   TITLE: Options pattern in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for binding the RaceDayApi settings from
   appsettings.json into a strongly typed options class.
*/


namespace RaceDay.Web.Services
{
    // Holds the settings used to reach the Part 2 RaceDay API.
    // Values come from appsettings.json and can be overridden by
    // environment variables such as RaceDayApi__BaseUrl.
    public class RaceDayApiOptions
    {
        // Name of the configuration section.
        public const string SectionName = "RaceDayApi";

        // Address of the API, for example https://localhost:7226/
        public string BaseUrl { get; set; } = string.Empty;

        // Development-only switch for Docker, where the API's
        // development certificate cannot be verified.
        public bool AllowInvalidCertificate { get; set; }
    }
}