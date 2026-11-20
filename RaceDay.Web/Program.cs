/*
   TITLE: Make HTTP requests using IHttpClientFactory in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for registering the typed HttpClient and
   configuring its base address, timeout and message handlers.


   TITLE: Session and state management in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for configuring session cookies, the idle timeout
   and the session middleware order.


   TITLE: Configure ASP.NET Core Localization
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/select-language-culture
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for fixing the request culture so decimal numbers
   and dates are read the same way on every computer.
*/

using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RaceDay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

/* ============================================================
   SECTION 1: MVC SERVICES
   ============================================================ */

builder.Services.AddControllersWithViews();


/* ============================================================
   SECTION 2: RACEDAY API SETTINGS
   ============================================================ */

// Bind the "RaceDayApi" section of appsettings.json.
builder.Services.Configure<RaceDayApiOptions>(
    builder.Configuration.GetSection(RaceDayApiOptions.SectionName));


/* ============================================================
   SECTION 3: SESSION
   ============================================================ */

builder.Services.AddHttpContextAccessor();

// Session data is kept in server memory.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Shorter than the API's 30 minutes, so MVC expires first.
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});


/* ============================================================
   SECTION 4: API CLIENT
   ============================================================ */

// Forwards each user's own API session cookie to the API.
builder.Services.AddTransient<ApiSessionCookieHandler>();

builder.Services
    .AddHttpClient<IRaceDayApiClient, RaceDayApiClient>(
        (serviceProvider, httpClient) =>
        {
            RaceDayApiOptions options = serviceProvider
                .GetRequiredService<IOptions<RaceDayApiOptions>>()
                .Value;

            if (!Uri.TryCreate(
                    options.BaseUrl,
                    UriKind.Absolute,
                    out Uri? baseAddress))
            {
                throw new InvalidOperationException(
                    "RaceDayApi:BaseUrl must be an absolute URL.");
            }

            httpClient.BaseAddress = baseAddress;
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        })
    .AddHttpMessageHandler<ApiSessionCookieHandler>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        RaceDayApiOptions options = serviceProvider
            .GetRequiredService<IOptions<RaceDayApiOptions>>()
            .Value;

        // UseCookies = false: cookies are never shared between users.
        // AllowAutoRedirect = false: redirects are reported, not hidden.
        HttpClientHandler handler = new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false
        };

        // Development and Docker only.
        if (options.AllowInvalidCertificate)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler
                    .DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });


/* ============================================================
   SECTION 5: REQUEST PIPELINE
   ============================================================ */

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Always read numbers and dates with en-US formatting, so a decimal
// such as 5.5 is understood on computers that use a comma decimal
// separator and inside Docker containers.
CultureInfo[] supportedCultures = { new CultureInfo("en-US") };

RequestLocalizationOptions localizationOptions =
    new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture("en-US"),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures
    };

localizationOptions.RequestCultureProviders.Clear();

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

// Session must run before the controllers that use it.
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();