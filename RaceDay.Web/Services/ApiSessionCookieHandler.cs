/*
   TITLE: DelegatingHandler Class
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for creating a message handler that adds the
   stored RaceDay API session cookie to outgoing requests and
   captures the cookie from API responses.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for storing each user's API session cookie in
   their own server-side MVC session.
*/

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Services
{
    // Forwards each user's own API session cookie to the Part 2 API
    // and saves any new API session cookie the API sends back.
    // The API cookie is kept in the MVC session, never in the browser.
    public class ApiSessionCookieHandler : DelegatingHandler
    {
        // The name of the session cookie issued by the Part 2 API.
        public const string ApiCookieName = ".RaceDay.Session";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiSessionCookieHandler(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ISession? session = _httpContextAccessor.HttpContext?.Session;

            // Add this user's API cookie to the outgoing request.
            if (session != null)
            {
                await session.LoadAsync(cancellationToken);

                string? storedCookie =
                    session.GetString(SessionKeys.ApiSessionCookie);

                if (!string.IsNullOrEmpty(storedCookie))
                {
                    request.Headers.TryAddWithoutValidation(
                        "Cookie",
                        ApiCookieName + "=" + storedCookie);
                }
            }

            HttpResponseMessage response =
                await base.SendAsync(request, cancellationToken);

            // Save or remove the API cookie based on the response.
            if (session != null)
            {
                CaptureApiCookie(response, session);
            }

            return response;
        }

        // Reads Set-Cookie headers and updates the stored API cookie.
        private static void CaptureApiCookie(
            HttpResponseMessage response,
            ISession session)
        {
            if (!response.Headers.TryGetValues(
                    "Set-Cookie",
                    out IEnumerable<string>? headerValues))
            {
                return;
            }

            foreach (string headerValue in headerValues)
            {
                // Keep only the "name=value" part before the first ";".
                string pair = headerValue.Split(';', 2)[0];

                int equalsIndex = pair.IndexOf('=');

                if (equalsIndex <= 0)
                {
                    continue;
                }

                string name = pair.Substring(0, equalsIndex).Trim();

                if (!string.Equals(
                        name, ApiCookieName, StringComparison.Ordinal))
                {
                    continue;
                }

                string value = pair.Substring(equalsIndex + 1).Trim();

                // The API deletes its cookie on logout by sending
                // an empty value, so remove the stored copy.
                if (value.Length == 0)
                {
                    session.Remove(SessionKeys.ApiSessionCookie);
                }
                else
                {
                    session.SetString(
                        SessionKeys.ApiSessionCookie, value);
                }
            }
        }
    }
}