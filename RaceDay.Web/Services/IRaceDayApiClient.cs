/*
   TITLE: Make HTTP requests using IHttpClientFactory in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for defining the typed client interface used by
   MVC controllers to call the RaceDay API.
*/


using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services
{
    // The only way MVC controllers talk to the Part 2 API.
    // Paths are relative to the configured base address,
    // for example "api/events".
    public interface IRaceDayApiClient
    {
        // Sends a GET request and returns the deserialised response.
        Task<ApiResult<TResponse>> GetAsync<TResponse>(
            string path,
            CancellationToken cancellationToken = default);

        // Sends a POST request with a JSON body.
        Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
            string path,
            TRequest body,
            CancellationToken cancellationToken = default);

        // Sends a POST request without a body (for example logout).
        Task<ApiResult> PostWithoutBodyAsync(
            string path,
            CancellationToken cancellationToken = default);

        // Sends a POST request that uploads one file as
        // multipart/form-data (the form field is named "file").
        Task<ApiResult<TResponse>> PostFileAsync<TResponse>(
            string path,
            IFormFile file,
            CancellationToken cancellationToken = default);

        // Sends a PUT request with a JSON body.
        Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
            string path,
            TRequest body,
            CancellationToken cancellationToken = default);

        // Sends a DELETE request.
        Task<ApiResult> DeleteAsync(
            string path,
            CancellationToken cancellationToken = default);
    }
}