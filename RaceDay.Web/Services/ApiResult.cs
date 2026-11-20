/*
   TITLE: Generic classes and methods (C# programming guide)
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for designing the generic ApiResult<T> class that
   wraps the outcome of each call to the RaceDay API.
*/

using System.Collections.Generic;

namespace RaceDay.Web.Services
{
    // Describes the outcome of one call to the RaceDay API.
    // Controllers read this instead of handling exceptions or raw JSON.
    public class ApiResult
    {
        // True when the API returned a 2xx status code.
        public bool Success { get; }

        // The HTTP status code returned by the API.
        public int StatusCode { get; }

        // A user-friendly message when the call failed.
        public string ErrorMessage { get; }

        // Field-level validation messages (property name to messages).
        public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

        // True when the user's API session had expired.
        public bool SessionExpired { get; }

        public bool IsUnauthorized => StatusCode == 401;

        public bool IsForbidden => StatusCode == 403;

        public bool IsNotFound => StatusCode == 404;

        public bool IsConflict => StatusCode == 409;

        protected ApiResult(
            bool success,
            int statusCode,
            string? errorMessage,
            IReadOnlyDictionary<string, string[]>? validationErrors,
            bool sessionExpired)
        {
            Success = success;
            StatusCode = statusCode;
            ErrorMessage = errorMessage ?? string.Empty;
            ValidationErrors = validationErrors
                ?? new Dictionary<string, string[]>();
            SessionExpired = sessionExpired;
        }

        // Creates a successful result that carries no data.
        public static ApiResult Succeeded(int statusCode)
        {
            return new ApiResult(true, statusCode, null, null, false);
        }

        // Creates a failed result.
        public static ApiResult Failed(
            int statusCode,
            string errorMessage,
            IReadOnlyDictionary<string, string[]>? validationErrors = null,
            bool sessionExpired = false)
        {
            return new ApiResult(
                false,
                statusCode,
                errorMessage,
                validationErrors,
                sessionExpired);
        }
    }

    // An API result that also carries the deserialised response data.
    public class ApiResult<T> : ApiResult
    {
        // The response data. Null when the call failed.
        public T? Data { get; }

        private ApiResult(
            bool success,
            int statusCode,
            T? data,
            string? errorMessage,
            IReadOnlyDictionary<string, string[]>? validationErrors,
            bool sessionExpired)
            : base(
                success,
                statusCode,
                errorMessage,
                validationErrors,
                sessionExpired)
        {
            Data = data;
        }

        // Creates a successful result with data.
        public static ApiResult<T> Succeeded(T data, int statusCode)
        {
            return new ApiResult<T>(
                true, statusCode, data, null, null, false);
        }

        // Converts a non-generic failure into a typed failure.
        public static ApiResult<T> FailedFrom(ApiResult failure)
        {
            return new ApiResult<T>(
                false,
                failure.StatusCode,
                default,
                failure.ErrorMessage,
                failure.ValidationErrors,
                failure.SessionExpired);
        }
    }
}