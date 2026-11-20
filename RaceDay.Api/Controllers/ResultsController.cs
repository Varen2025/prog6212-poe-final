using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceEnrolment = RaceDay.Api.Models.Enrolment;
using RaceEvent = RaceDay.Api.Models.Event;
using RaceResult = RaceDay.Api.Models.Result;

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

       Usage: Referenced for implementing race result endpoints
       and returning the correct HTTP response codes.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for protecting Organiser and
       Participant result operations.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for performing asynchronous
       result queries and database updates.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Querying Data - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/querying/
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for counting related enrolments inside a
       query projection to calculate the total finishers.

       ============================================================ */

    // Manages event results and published race history.
    [ApiController]
    public class ResultsController : ControllerBase
    {
        // Database context used to access RaceDay records.
        private readonly RaceDayDbContext _context;

        // Receive the database context through dependency injection.
        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }


        /* ========================================================
           SECTION 1: GET AUTHENTICATED USER ID
           ======================================================== */

        // Read the ID verified by our authentication handler.
        private int? GetCurrentUserId()
        {
            string? value = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(value, out int userId) &&
                userId > 0)
            {
                return userId;
            }

            return null;
        }


        /* ========================================================
           SECTION 2: BUILD RESULT RESPONSE QUERY
           ======================================================== */

        // Join the Result, Enrolment, Event, Category and
        // User tables to return useful race result details.
        private IQueryable<ResultResponseDto> BuildResultQuery()
        {
            var query =
                from result in _context.Results.AsNoTracking()

                join enrolment in _context.Enrolments.AsNoTracking()
                    on result.EnrolmentId equals enrolment.EnrolmentId

                join raceEvent in _context.Events.AsNoTracking()
                    on enrolment.EventId equals raceEvent.EventId

                join category in _context.Categories.AsNoTracking()
                    on enrolment.CategoryId equals category.CategoryId

                join participant in _context.Users.AsNoTracking()
                    on enrolment.ParticipantId equals participant.UserId

                select new ResultResponseDto
                {
                    ResultId = result.ResultId,

                    EnrolmentId = result.EnrolmentId,

                    ParticipantId = enrolment.ParticipantId,

                    ParticipantName =
                        participant.FirstName + " " +
                        participant.LastName,

                    EventId = enrolment.EventId,

                    EventName = raceEvent.Name,

                    EventDate = raceEvent.EventDate,

                    CategoryId = enrolment.CategoryId,

                    CategoryName = category.CategoryName,

                    FinishTime = result.FinishTime,

                    FinishingPosition = result.FinishingPosition,

                    IsPublished = result.IsPublished,

                    PublishedAt = result.PublishedAt,

                    // Count the finishers (Completed enrolments)
                    // in the same event as this result.
                    TotalFinishers = _context.Enrolments
                        .Count(entry =>
                            entry.EventId == enrolment.EventId &&
                            entry.Status == "Completed")
                };

            return query;
        }


        /* ========================================================
           SECTION 3: RECORD A RACE RESULT
           POST /api/results
           ======================================================== */

        /// <summary>
        /// Records the finishing time and position for an enrolment.
        /// </summary>
        /// <remarks>
        /// Only the Organiser who owns the event may record results.
        /// The event must have started and the enrolment must
        /// be marked Completed.
        /// </remarks>
        [HttpPost("/api/results")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(ResultResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult(
            [FromBody] ResultRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 3.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 3.2: FIND THE ENROLMENT

            RaceEnrolment? enrolment =
                await _context.Enrolments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.EnrolmentId ==
                            request.EnrolmentId,
                        cancellationToken);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "RaceDay enrolment not found."
                });
            }


            // SECTION 3.3: FIND THE ASSOCIATED EVENT

            RaceEvent? selectedEvent =
                await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.EventId == enrolment.EventId,
                        cancellationToken);

            if (selectedEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 3.4: VERIFY EVENT OWNERSHIP

            // Organisers may only record results for
            // events that belong to their own accounts.
            if (selectedEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 3.5: CHECK THE EVENT DATE

            // Results are only recorded after an event starts.
            // The current project stores local event dates.
            if (selectedEvent.EventDate > DateTime.Now)
            {
                return BadRequest(new
                {
                    message =
                        "Results cannot be recorded before " +
                        "the event starts."
                });
            }


            // SECTION 3.6: VALIDATE ENROLMENT STATUS

            // Require the event entry to be marked Completed.
            if (enrolment.Status != "Completed")
            {
                return BadRequest(new
                {
                    message =
                        "The enrolment must be marked Completed " +
                        "before a result can be recorded."
                });
            }


            // SECTION 3.7: CHECK FOR AN EXISTING RESULT

            // Each enrolment may have only one result.
            bool resultExists = await _context.Results
                .AnyAsync(
                    item => item.EnrolmentId ==
                        request.EnrolmentId,
                    cancellationToken);

            if (resultExists)
            {
                return Conflict(new
                {
                    message =
                        "A result already exists for this enrolment."
                });
            }


            // SECTION 3.8: CREATE THE RESULT ENTITY

            // The DTO validation requires a finishing time
            // and an explicit publication status.
            bool publishResult = request.IsPublished!.Value;

            RaceResult newResult = new RaceResult
            {
                EnrolmentId = request.EnrolmentId,

                FinishTime = request.FinishTime!.Value,

                FinishingPosition = request.FinishingPosition,

                IsPublished = publishResult,

                // A published result must have a publication date.
                // An unpublished result must have a null date.
                PublishedAt = publishResult
                    ? DateTime.Now
                    : null
            };


            // SECTION 3.9: SAVE RESULT TO SQL SERVER

            _context.Results.Add(newResult);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException
                    sqlException &&
                    (sqlException.Number == 2601 ||
                     sqlException.Number == 2627))
            {
                // The database also enforces one result per
                // enrolment, protecting concurrent requests.
                return Conflict(new
                {
                    message =
                        "A result already exists for this enrolment."
                });
            }


            // SECTION 3.10: RETRIEVE THE RESULT DETAILS

            ResultResponseDto response =
                await BuildResultQuery()
                    .FirstAsync(
                        item => item.ResultId == newResult.ResultId,
                        cancellationToken);


            // SECTION 3.11: RETURN HTTP 201 CREATED

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }


        /* ========================================================
           SECTION 4: UPDATE AN EXISTING RESULT
           PUT /api/results/{id}
           ======================================================== */

        /// <summary>
        /// Corrects or publishes a result owned by the Organiser.
        /// </summary>
        [HttpPut("/api/results/{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(ResultResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(
            int id,
            [FromBody] UpdateResultRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 4.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 4.2: FIND THE RESULT

            RaceResult? existingResult =
                await _context.Results.FirstOrDefaultAsync(
                    item => item.ResultId == id,
                    cancellationToken);

            if (existingResult == null)
            {
                return NotFound(new
                {
                    message = "RaceDay result not found."
                });
            }


            // SECTION 4.3: FIND THE RELATED ENROLMENT

            RaceEnrolment? enrolment =
                await _context.Enrolments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.EnrolmentId ==
                            existingResult.EnrolmentId,
                        cancellationToken);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "RaceDay enrolment not found."
                });
            }


            // SECTION 4.4: VERIFY EVENT OWNERSHIP

            // Check the event linked to this enrolment.
            bool ownsEvent = await _context.Events
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.EventId == enrolment.EventId &&
                        item.OrganiserId == organiserId.Value,
                    cancellationToken);

            if (!ownsEvent)
            {
                return Forbid();
            }


            // SECTION 4.5: UPDATE THE FINISHING DETAILS

            // Correct the finishing time if necessary.
            existingResult.FinishTime =
                request.FinishTime!.Value;

            // Correct the finishing position.
            existingResult.FinishingPosition =
                request.FinishingPosition;


            // SECTION 4.6: UPDATE PUBLICATION STATUS

            bool shouldPublish = request.IsPublished!.Value;

            // Set a date when first publishing a result.
            if (shouldPublish)
            {
                if (!existingResult.IsPublished ||
                    existingResult.PublishedAt == null)
                {
                    existingResult.PublishedAt = DateTime.Now;
                }
            }
            else
            {
                // Clear the publication date when unpublishing.
                existingResult.PublishedAt = null;
            }

            // Save the new publication status.
            existingResult.IsPublished = shouldPublish;


            // SECTION 4.7: SAVE THE CORRECTION

            await _context.SaveChangesAsync(
                cancellationToken);


            // SECTION 4.8: RETURN UPDATED RESULT

            ResultResponseDto response =
                await BuildResultQuery()
                    .FirstAsync(
                        item => item.ResultId == id,
                        cancellationToken);

            return Ok(response);
        }


        /* ========================================================
           SECTION 5: GET RESULTS FOR AN ORGANISER'S EVENT
           GET /api/events/{eventId}/results
           ======================================================== */

        /// <summary>
        /// Returns results for an event owned by the Organiser.
        /// </summary>
        /// <remarks>
        /// Organisers can see published and unpublished results
        /// for their own events.
        /// </remarks>
        [HttpGet("/api/events/{eventId:int}/results")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(
            int eventId,
            CancellationToken cancellationToken)
        {
            // SECTION 5.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 5.2: FIND THE EVENT

            RaceEvent? selectedEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.EventId == eventId,
                    cancellationToken);

            if (selectedEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 5.3: VERIFY EVENT OWNERSHIP

            if (selectedEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 5.4: RETRIEVE THE EVENT RESULTS

            // Organisers can review results before publication.
            var results = await BuildResultQuery()
                .Where(item => item.EventId == eventId)
                .OrderBy(item => item.FinishingPosition)
                .ToListAsync(cancellationToken);


            // SECTION 5.5: RETURN HTTP 200 OK

            return Ok(results);
        }


        /* ========================================================
           SECTION 6: GET MY PUBLISHED RACE RESULTS
           GET /api/results/me
           ======================================================== */

        /// <summary>
        /// Returns published results for the logged-in Participant.
        /// </summary>
        [HttpGet("/api/results/me")]
        [Authorize(Policy = "ParticipantOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults(
            CancellationToken cancellationToken)
        {
            // SECTION 6.1: IDENTIFY THE PARTICIPANT

            int? participantId = GetCurrentUserId();

            if (participantId == null)
            {
                return Unauthorized();
            }


            // SECTION 6.2: RETRIEVE PUBLISHED RESULTS

            // Only include results owned by this Participant.
            // Unpublished results are never returned.
            var results = await BuildResultQuery()
                .Where(item =>
                    item.ParticipantId == participantId.Value &&
                    item.IsPublished)
                .OrderByDescending(item => item.EventDate)
                .ToListAsync(cancellationToken);


            // SECTION 6.3: RETURN HTTP 200 OK

            return Ok(results);
        }
    }
}