
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers
{
    /* ============================================================
       Code Attribution

       TITLE: Create Web APIs with ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/web-api/
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for implementing the RaceDay
       Organiser dashboard RESTful API endpoint.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting dashboard access
       to authenticated Organisers.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Complex Query Operators - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for joining event and enrolment
       records and grouping results to calculate totals.

       ============================================================ */

    // Provides dashboard information for RaceDay Organisers.
    [ApiController]
    [Route("api/organiser")]
    [Authorize(Policy = "OrganiserOnly")]
    public class OrganiserController : ControllerBase
    {
        /* ========================================================
           SECTION 1: DATABASE CONTEXT
           ======================================================== */

        // Database context for retrieving RaceDay information.
        private readonly RaceDayDbContext _context;

        // Receive the database context through dependency injection.
        public OrganiserController(RaceDayDbContext context)
        {
            _context = context;
        }


        /* ========================================================
           SECTION 2: GET AUTHENTICATED ORGANISER ID
           ======================================================== */

        // Identify the logged-in Organiser from the session.
        // Never accept an Organiser ID from the request body.
        private int? GetCurrentOrganiserId()
        {
            // Read the identifier supplied by authentication.
            string? userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            // Confirm the identifier is a positive integer.
            if (int.TryParse(userIdValue, out int organiserId)
                && organiserId > 0)
            {
                return organiserId;
            }

            // Return null if authentication data is invalid.
            return null;
        }


        /* ========================================================
           SECTION 3: GET ORGANISER DASHBOARD
           GET /api/organiser/dashboard
           ======================================================== */

        /// <summary>
        /// Returns the logged-in Organiser's dashboard summary.
        /// </summary>
        /// <remarks>
        /// Includes owned events, upcoming event dates
        /// and the number of enrolments for each event.
        ///
        /// The dashboard does not expose events created
        /// by other Organisers.
        /// </remarks>
        [HttpGet("dashboard")]
        [ProducesResponseType(
            typeof(OrganiserDashboardDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboard(
            CancellationToken cancellationToken)
        {
            /* ====================================================
               SECTION 3.1: IDENTIFY THE ORGANISER
               ==================================================== */

            // Obtain the logged-in Organiser's user ID.
            int? organiserId = GetCurrentOrganiserId();

            // Reject requests without a valid user identifier.
            if (organiserId == null)
            {
                return Unauthorized(new
                {
                    message = "You must be logged in."
                });
            }


            /* ====================================================
               SECTION 3.2: RETRIEVE OWNED EVENTS
               ==================================================== */

            // Get the current server-local date and time.
            // RaceDay currently stores event dates in local time.
            DateTime now = DateTime.Now;

            // Retrieve only events created by this Organiser.
            // AsNoTracking improves performance for read-only data.
            var ownedEvents = await _context.Events
                .AsNoTracking()
                .Where(raceEvent =>
                    raceEvent.OrganiserId == organiserId.Value)
                .OrderBy(raceEvent => raceEvent.EventDate)
                .Select(raceEvent => new
                {
                    raceEvent.EventId,
                    raceEvent.Name,
                    raceEvent.EventDate,
                    raceEvent.Location,
                    raceEvent.EventType
                })
                .ToListAsync(cancellationToken);


            /* ====================================================
               SECTION 3.3: CALCULATE ENROLMENT COUNTS
               ==================================================== */

            // Join enrolments to their corresponding events.
            // The ownership filter prevents counting enrolments
            // from events created by other Organisers.
            var enrolmentStatistics = await
                (
                    from enrolment in
                        _context.Enrolments.AsNoTracking()

                    join raceEvent in
                        _context.Events.AsNoTracking()

                    on enrolment.EventId equals raceEvent.EventId

                    where raceEvent.OrganiserId ==
                        organiserId.Value

                    // Group enrolments by their event ID.
                    group enrolment by enrolment.EventId
                    into eventGroup

                    select new
                    {
                        // Event associated with the enrolments.
                        EventId = eventGroup.Key,

                        // Count every enrolment record.
                        TotalEnrolments = eventGroup.Count(),

                        // Count Pending enrolments.
                        PendingEnrolments = eventGroup.Count(
                            item => item.Status == "Pending"),

                        // Count Confirmed enrolments.
                        ConfirmedEnrolments = eventGroup.Count(
                            item => item.Status == "Confirmed"),

                        // Count Completed enrolments.
                        CompletedEnrolments = eventGroup.Count(
                            item => item.Status == "Completed"),

                        // Count Cancelled enrolments.
                        CancelledEnrolments = eventGroup.Count(
                            item => item.Status == "Cancelled")
                    }
                )
                .ToListAsync(cancellationToken);


            /* ====================================================
               SECTION 3.4: MATCH STATISTICS TO EVENTS
               ==================================================== */

            // Use a dictionary to match each event to its counts.
            // This avoids repeatedly searching the statistics list.
            var countsByEventId = enrolmentStatistics
                .ToDictionary(item => item.EventId);

            // Build a dashboard entry for each owned event.
            var dashboardEvents = ownedEvents
                .Select(raceEvent =>
                {
                    // Locate enrolment counts for this event.
                    countsByEventId.TryGetValue(
                        raceEvent.EventId,
                        out var counts);

                    // An event without enrolments has zero counts.
                    return new DashboardEventDto
                    {
                        EventId = raceEvent.EventId,

                        Name = raceEvent.Name,

                        EventDate = raceEvent.EventDate,

                        Location = raceEvent.Location,

                        EventType = raceEvent.EventType,

                        IsUpcoming =
                            raceEvent.EventDate >= now,

                        TotalEnrolments =
                            counts?.TotalEnrolments ?? 0,

                        PendingEnrolments =
                            counts?.PendingEnrolments ?? 0,

                        ConfirmedEnrolments =
                            counts?.ConfirmedEnrolments ?? 0,

                        CompletedEnrolments =
                            counts?.CompletedEnrolments ?? 0,

                        CancelledEnrolments =
                            counts?.CancelledEnrolments ?? 0
                    };
                })
                .ToList();


            /* ====================================================
               SECTION 3.5: CALCULATE OVERALL EVENT TOTALS
               ==================================================== */

            // Count all events owned by this Organiser.
            int totalEvents = dashboardEvents.Count;

            // Count the events that have not started yet.
            int upcomingEvents = dashboardEvents.Count(
                item => item.IsUpcoming);

            // Count events whose start time has passed.
            int pastEvents = dashboardEvents.Count(
                item => !item.IsUpcoming);


            /* ====================================================
               SECTION 3.6: CALCULATE TOTAL ENROLMENTS
               ==================================================== */

            // Add the enrolments from all owned events.
            int totalEnrolments = dashboardEvents.Sum(
                item => item.TotalEnrolments);


            /* ====================================================
               SECTION 3.7: FIND THE NEXT UPCOMING EVENT
               ==================================================== */

            // Select the earliest future event date.
            // FirstOrDefault returns null when none exists.
            DateTime? nextUpcomingEventDate = dashboardEvents
                .Where(item => item.IsUpcoming)
                .OrderBy(item => item.EventDate)
                .Select(item => (DateTime?)item.EventDate)
                .FirstOrDefault();


            /* ====================================================
               SECTION 3.8: PREPARE DASHBOARD RESPONSE
               ==================================================== */

            // Combine the overall totals and event breakdowns.
            OrganiserDashboardDto dashboard =
                new OrganiserDashboardDto
                {
                    OrganiserId = organiserId.Value,

                    TotalEvents = totalEvents,

                    UpcomingEvents = upcomingEvents,

                    PastEvents = pastEvents,

                    TotalEnrolments = totalEnrolments,

                    NextUpcomingEventDate =
                        nextUpcomingEventDate,

                    Events = dashboardEvents
                };


            /* ====================================================
               SECTION 3.9: RETURN HTTP 200 OK
               ==================================================== */

            // Return the dashboard summary as JSON.
            return Ok(dashboard);
        }
    }
}
