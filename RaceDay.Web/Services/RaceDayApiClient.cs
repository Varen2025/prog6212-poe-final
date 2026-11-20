/*
   TITLE: Make HTTP requests using IHttpClientFactory in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for the typed HttpClient that sends requests to
   the RaceDay API.


   TITLE: How to serialize and deserialize JSON in .NET
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for reading the API's JSON responses and error
   messages with System.Text.Json.


   TITLE: MultipartFormDataContent Class
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/api/system.net.http.multipartformdatacontent
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for forwarding an uploaded image to the RaceDay
   API as multipart/form-data.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RaceDay.Web.Services
{
    // Sends requests to the Part 2 RaceDay API and converts every
    // outcome into an ApiResult with a user-friendly message.
    public class RaceDayApiClient : IRaceDayApiClient
    {
        private const string UnavailableMessage =
            "The RaceDay service is currently unavailable. " +
            "Please try again in a moment.";

        private const string UnexpectedResponseMessage =
            "The RaceDay service returned an unexpected response. " +
            "Please try again.";

        // Web defaults use camelCase names, matching the API's JSON.
        private static readonly JsonSerializerOptions JsonOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RaceDayApiClient> _logger;

        public RaceDayApiClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RaceDayApiClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        /* ========================================================
           SECTION 1: PUBLIC METHODS
           ======================================================== */

        public async Task<ApiResult<TResponse>> GetAsync<TResponse>(
            string path,
            CancellationToken cancellationToken = default)
        {
            return await SendForDataAsync<TResponse>(
                HttpMethod.Get, path, null, cancellationToken);
        }

        public async Task<ApiResult<TResponse>>
            PostAsync<TRequest, TResponse>(
                string path,
                TRequest body,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(body);

            return await SendForDataAsync<TResponse>(
                HttpMethod.Post, path, body, cancellationToken);
        }

        public async Task<ApiResult> PostWithoutBodyAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return await SendForStatusAsync(
                HttpMethod.Post, path, cancellationToken);
        }

        public async Task<ApiResult<TResponse>> PostFileAsync<TResponse>(
            string path,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            // Build a multipart body with one part named "file",
            // which is the parameter name the API expects.
            using MultipartFormDataContent content =
                new MultipartFormDataContent();

            StreamContent fileContent =
                new StreamContent(file.OpenReadStream());

            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                fileContent.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(file.ContentType);
            }

            content.Add(fileContent, "file", file.FileName);

            return await SendForDataAsync<TResponse>(
                HttpMethod.Post,
                path,
                null,
                cancellationToken,
                content);
        }

        public async Task<ApiResult<TResponse>>
            PutAsync<TRequest, TResponse>(
                string path,
                TRequest body,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(body);

            return await SendForDataAsync<TResponse>(
                HttpMethod.Put, path, body, cancellationToken);
        }

        public async Task<ApiResult> DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return await SendForStatusAsync(
                HttpMethod.Delete, path, cancellationToken);
        }


        /* ========================================================
           SECTION 2: SEND A REQUEST AND READ THE OUTCOME
           ======================================================== */

        // Sends a request and deserialises the successful response.
        private async Task<ApiResult<TResponse>>
            SendForDataAsync<TResponse>(
                HttpMethod method,
                string path,
                object? body,
                CancellationToken cancellationToken,
                HttpContent? content = null)
        {
            (HttpResponseMessage? response, ApiResult? failure) =
                await SendAsync(
                    method, path, body, cancellationToken, content);

            if (failure != null)
            {
                return ApiResult<TResponse>.FailedFrom(failure);
            }

            using (response)
            {
                try
                {
                    TResponse? data = await response!.Content
                        .ReadFromJsonAsync<TResponse>(
                            JsonOptions,
                            cancellationToken);

                    if (data == null)
                    {
                        return ApiResult<TResponse>.FailedFrom(
                            ApiResult.Failed(
                                502, UnexpectedResponseMessage));
                    }

                    return ApiResult<TResponse>.Succeeded(
                        data, (int)response.StatusCode);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Could not read the API response for {Path}.",
                        path);

                    return ApiResult<TResponse>.FailedFrom(
                        ApiResult.Failed(
                            502, UnexpectedResponseMessage));
                }
            }
        }

        // Sends a request that returns no data (204 No Content).
        private async Task<ApiResult> SendForStatusAsync(
            HttpMethod method,
            string path,
            CancellationToken cancellationToken)
        {
            (HttpResponseMessage? response, ApiResult? failure) =
                await SendAsync(method, path, null, cancellationToken);

            if (failure != null)
            {
                return failure;
            }

            using (response)
            {
                return ApiResult.Succeeded((int)response!.StatusCode);
            }
        }

        // Sends the HTTP request. Returns either a successful response
        // or a failure, never both, and never throws for HTTP errors.
        // A ready-made content (for example a file upload) is used
        // instead of a JSON body when one is supplied.
        private async Task<(HttpResponseMessage? Response,
            ApiResult? Failure)> SendAsync(
                HttpMethod method,
                string path,
                object? body,
                CancellationToken cancellationToken,
                HttpContent? content = null)
        {
            // Remember whether this user believed they were logged in.
            bool hadApiSession = HasStoredApiSession();

            try
            {
                using HttpRequestMessage request =
                    new HttpRequestMessage(method, path);

                if (content != null)
                {
                    request.Content = content;
                }
                else if (body != null)
                {
                    request.Content = JsonContent.Create(
                        body, body.GetType(), null, JsonOptions);
                }

                HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        request, cancellationToken);

                int statusCode = (int)response.StatusCode;

                // A redirect means the configured address is wrong,
                // for example http instead of https.
                if (statusCode >= 300 && statusCode < 400)
                {
                    response.Dispose();

                    _logger.LogWarning(
                        "The API redirected {Method} {Path}. " +
                        "Check that RaceDayApi:BaseUrl uses https.",
                        method,
                        path);

                    return (null, ApiResult.Failed(
                        502, UnavailableMessage));
                }

                if (response.IsSuccessStatusCode)
                {
                    return (response, null);
                }

                ApiResult failure = await BuildFailureAsync(
                    response, hadApiSession, cancellationToken);

                response.Dispose();

                return (null, failure);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not reach the API for {Method} {Path}.",
                    method,
                    path);

                return (null, ApiResult.Failed(
                    503, UnavailableMessage));
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "The API request timed out for {Method} {Path}.",
                    method,
                    path);

                return (null, ApiResult.Failed(
                    504, UnavailableMessage));
            }
        }


        /* ========================================================
           SECTION 3: CONVERT API ERRORS INTO FRIENDLY RESULTS
           ======================================================== */

        private async Task<ApiResult> BuildFailureAsync(
            HttpResponseMessage response,
            bool hadApiSession,
            CancellationToken cancellationToken)
        {
            int statusCode = (int)response.StatusCode;

            // The user had a session, but the API no longer accepts it.
            // Clear the MVC session so the user is sent to log in again.
            if (statusCode == 401 && hadApiSession)
            {
                _httpContextAccessor.HttpContext?.Session.Clear();

                return ApiResult.Failed(
                    statusCode,
                    "Your session has expired. Please log in again.",
                    null,
                    true);
            }

            string? message = null;

            Dictionary<string, string[]> validationErrors =
                new Dictionary<string, string[]>();

            string responseText = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try
                {
                    using JsonDocument document =
                        JsonDocument.Parse(responseText);

                    JsonElement root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        // Shape 1: validation errors from a 400 response.
                        if (root.TryGetProperty(
                                "errors", out JsonElement errorsElement) &&
                            errorsElement.ValueKind ==
                                JsonValueKind.Object)
                        {
                            foreach (JsonProperty property in
                                errorsElement.EnumerateObject())
                            {
                                if (property.Value.ValueKind !=
                                    JsonValueKind.Array)
                                {
                                    continue;
                                }

                                string[] messages = property.Value
                                    .EnumerateArray()
                                    .Where(item => item.ValueKind ==
                                        JsonValueKind.String)
                                    .Select(item =>
                                        item.GetString() ?? string.Empty)
                                    .Where(text => text.Length > 0)
                                    .ToArray();

                                if (messages.Length > 0)
                                {
                                    validationErrors[property.Name] =
                                        messages;
                                }
                            }
                        }

                        // Shape 2: { "message": "..." } business errors.
                        if (root.TryGetProperty(
                                "message", out JsonElement messageElement) &&
                            messageElement.ValueKind ==
                                JsonValueKind.String)
                        {
                            message = messageElement.GetString();
                        }
                    }
                }
                catch (JsonException)
                {
                    // The body was not JSON. Use a default message.
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = validationErrors.Count > 0
                    ? string.Join(
                        " ",
                        validationErrors.Values.SelectMany(item => item))
                    : DefaultMessageFor(statusCode);
            }

            return ApiResult.Failed(
                statusCode, message, validationErrors);
        }

        // Friendly messages for responses that have no body,
        // such as the API's 403 Forbidden.
        private static string DefaultMessageFor(int statusCode)
        {
            return statusCode switch
            {
                400 => "Please check the information you entered " +
                       "and try again.",
                401 => "Please log in to continue.",
                403 => "You do not have permission to do that.",
                404 => "The requested item could not be found.",
                409 => "That action conflicts with existing data.",
                _ => "Something went wrong. Please try again."
            };
        }

        // True when this user has an API session stored in MVC.
        private bool HasStoredApiSession()
        {
            string? stored = _httpContextAccessor.HttpContext?
                .Session.GetString(SessionKeys.ApiSessionCookie);

            return !string.IsNullOrEmpty(stored);
        }
    }
}