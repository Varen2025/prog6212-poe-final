
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
using RaceDay.Api.Models;

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
       enrolment endpoints and HTTP status responses.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting enrolment operations
       to the appropriate Organiser and Participant roles.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for querying and saving enrolments
       asynchronously using Entity Framework Core.

       ============================================================ */

    // Handles Participant enrolments and Organiser management.
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        // Database context for RaceDay records.
        private readonly RaceDayDbContext _context;

        // Receive the database context through dependency injection.
        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }


        /* ========================================================
           SECTION 1: GET THE AUTHENTICATED USER ID
           ======================================================== */

        // Read the verified user ID from the authentication claims.
        // This ID comes from the server-side login session.
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
           SECTION 2: PREPARE ENROLMENT RESPONSE QUERY
           ======================================================== */

        // Join the related tables to return meaningful details.
        // This allows Participants to see event and category names.
        // Organisers can also see the enrolled Participant's name.
        private IQueryable<EnrolmentResponseDto>
            BuildEnrolmentQuery()
        {
            var query =
                from enrolment in _context.Enrolments.AsNoTracking()

                join raceEvent in _context.Events.AsNoTracking()
                    on enrolment.EventId equals raceEvent.EventId

                join category in _context.Categories.AsNoTracking()
                    on enrolment.CategoryId equals category.CategoryId

                join participant in _context.Users.AsNoTracking()
                    on enrolment.ParticipantId equals participant.UserId

                select new EnrolmentResponseDto
                {
                    EnrolmentId = enrolment.EnrolmentId,

                    ParticipantId = enrolment.ParticipantId,

                    ParticipantName =
                        participant.FirstName + " " +
                        participant.LastName,

                    EventId = enrolment.EventId,

                    EventName = raceEvent.Name,

                    EventDate = raceEvent.EventDate,

                    CategoryId = enrolment.CategoryId,

                    CategoryName = category.CategoryName,

                    EnrolmentDate = enrolment.EnrolmentDate,

                    Status = enrolment.Status
                };

            return query;
        }


        /* ========================================================
           SECTION 3: CREATE A PARTICIPANT ENROLMENT
           POST /api/enrolments
           ======================================================== */

        /// <summary>
        /// Enrols the logged-in Participant into an event.
        /// </summary>
        /// <remarks>
        /// Validates the event, selected category, age
        /// eligibility and existing enrolments before saving.
        /// </remarks>
        [HttpPost("/api/enrolments")]
        [Authorize(Policy = "ParticipantOnly")]
        [ProducesResponseType(
            typeof(EnrolmentResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateEnrolment(
            [FromBody] EnrolmentRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 3.1: IDENTIFY THE PARTICIPANT

            int? participantId = GetCurrentUserId();

            if (participantId == null)
            {
                return Unauthorized();
            }


            // SECTION 3.2: FIND THE SELECTED EVENT

            Event? selectedEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.EventId == request.EventId,
                    cancellationToken);

            if (selectedEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 3.3: CHECK THE EVENT DATE

            // Only allow enrolments before the event starts.
            // RaceDay currently stores local event date/times.
            if (selectedEvent.EventDate <= DateTime.Now)
            {
                return BadRequest(new
                {
                    message =
                        "Enrolment is closed for this event."
                });
            }


            // SECTION 3.4: FIND THE SELECTED CATEGORY

            Category? selectedCategory =
                await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.CategoryId == request.CategoryId,
                        cancellationToken);

            if (selectedCategory == null)
            {
                return NotFound(new
                {
                    message = "RaceDay category not found."
                });
            }


            // SECTION 3.5: VALIDATE THE EVENT AND CATEGORY

            // The category must belong to the selected event.
            if (selectedCategory.EventId != request.EventId)
            {
                return BadRequest(new
                {
                    message =
                        "The selected category does not belong " +
                        "to the selected event."
                });
            }


            // SECTION 3.6: GET THE PARTICIPANT'S ACCOUNT

            User? participant = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.UserId == participantId.Value,
                    cancellationToken);

            if (participant == null)
            {
                return Unauthorized();
            }


            // SECTION 3.7: VALIDATE AGE-BASED ELIGIBILITY

            // Calculate the Participant's age on the event date.
            // This is more appropriate than using today's age.
            if (selectedCategory.CategoryType == "Age")
            {
                if (!selectedCategory.MinAge.HasValue ||
                    !selectedCategory.MaxAge.HasValue)
                {
                    return BadRequest(new
                    {
                        message =
                            "The selected age category is invalid."
                    });
                }

                DateOnly eventDate =
                    DateOnly.FromDateTime(selectedEvent.EventDate);

                int age = eventDate.Year -
                    participant.DateOfBirth.Year;

                // Adjust the age if the Participant has not
                // reached their birthday by the event date.
                if (participant.DateOfBirth >
                    eventDate.AddYears(-age))
                {
                    age--;
                }

                // Only eligible Participants may enter.
                if (age < selectedCategory.MinAge.Value ||
                    age > selectedCategory.MaxAge.Value)
                {
                    return BadRequest(new
                    {
                        message =
                            "You do not meet the age requirements " +
                            "for this category."
                    });
                }
            }


            // SECTION 3.8: VALIDATE DISTANCE CATEGORIES

            // A distance category needs a valid distance.
            if (selectedCategory.CategoryType == "Distance")
            {
                if (!selectedCategory.DistanceKm.HasValue ||
                    selectedCategory.DistanceKm.Value <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "The selected distance category is invalid."
                    });
                }
            }

            // Reject unsupported category types.
            if (selectedCategory.CategoryType != "Age" &&
                selectedCategory.CategoryType != "Distance")
            {
                return BadRequest(new
                {
                    message = "Invalid category type."
                });
            }


            // SECTION 3.9: PREVENT DUPLICATE ENROLMENTS

            // A Participant may only enter each event once.
            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(
                    enrolment =>
                        enrolment.ParticipantId ==
                            participantId.Value &&
                        enrolment.EventId == request.EventId,
                    cancellationToken);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message =
                        "You are already enrolled in this event."
                });
            }


            // SECTION 3.10: CREATE THE ENROLMENT RECORD

            Enrolment newEnrolment = new Enrolment
            {
                // Assign the logged-in Participant.
                ParticipantId = participantId.Value,

                // Store the selected event.
                EventId = request.EventId,

                // Store the selected category.
                CategoryId = request.CategoryId,

                // New enrolments start as Pending.
                Status = "Pending"

                // EnrolmentDate uses the database default.
            };


            // SECTION 3.11: SAVE THE ENROLMENT

            _context.Enrolments.Add(newEnrolment);

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
                // Handle simultaneous duplicate requests.
                return Conflict(new
                {
                    message =
                        "You are already enrolled in this event."
                });
            }


            // SECTION 3.12: PREPARE THE RESPONSE

            EnrolmentResponseDto response =
                new EnrolmentResponseDto
                {
                    EnrolmentId = newEnrolment.EnrolmentId,

                    ParticipantId = participantId.Value,

                    ParticipantName =
                        participant.FirstName + " " +
                        participant.LastName,

                    EventId = selectedEvent.EventId,

                    EventName = selectedEvent.Name,

                    EventDate = selectedEvent.EventDate,

                    CategoryId = selectedCategory.CategoryId,

                    CategoryName = selectedCategory.CategoryName,

                    EnrolmentDate = newEnrolment.EnrolmentDate,

                    Status = newEnrolment.Status
                };


            // SECTION 3.13: RETURN HTTP 201 CREATED

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }


        /* ========================================================
           SECTION 4: GET MY ENROLMENTS
           GET /api/enrolments/me
           ======================================================== */

        /// <summary>
        /// Returns enrolments for the logged-in Participant.
        /// </summary>
        [HttpGet("/api/enrolments/me")]
        [Authorize(Policy = "ParticipantOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments(
            CancellationToken cancellationToken)
        {
            // SECTION 4.1: IDENTIFY THE PARTICIPANT

            int? participantId = GetCurrentUserId();

            if (participantId == null)
            {
                return Unauthorized();
            }


            // SECTION 4.2: RETRIEVE PERSONAL ENROLMENTS

            // Only records belonging to this Participant
            // will be returned by the database query.
            var enrolments = await BuildEnrolmentQuery()
                .Where(item =>
                    item.ParticipantId == participantId.Value)
                .OrderBy(item => item.EventDate)
                .ToListAsync(cancellationToken);


            // SECTION 4.3: RETURN HTTP 200 OK

            return Ok(enrolments);
        }


        /* ========================================================
           SECTION 5: GET ENROLMENTS FOR AN ORGANISER'S EVENT
           GET /api/events/{eventId}/enrolments
           ======================================================== */

        /// <summary>
        /// Returns Participants enrolled in an Organiser's event.
        /// </summary>
        [HttpGet("/api/events/{eventId:int}/enrolments")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(
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

            Event? selectedEvent = await _context.Events
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

            // Organisers must not access another Organiser's
            // private Participant enrolment information.
            if (selectedEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 5.4: RETRIEVE EVENT ENROLMENTS

            var enrolments = await BuildEnrolmentQuery()
                .Where(item => item.EventId == eventId)
                .OrderBy(item => item.ParticipantName)
                .ToListAsync(cancellationToken);


            // SECTION 5.5: RETURN HTTP 200 OK

            return Ok(enrolments);
        }


        /* ========================================================
           SECTION 6: UPDATE AN ENROLMENT STATUS
           PUT /api/enrolments/{id}/status
           ======================================================== */

        /// <summary>
        /// Updates an enrolment for an event owned by the Organiser.
        /// </summary>
        [HttpPut("/api/enrolments/{id:int}/status")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(EnrolmentResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEnrolmentStatus(
            int id,
            [FromBody] UpdateEnrolmentStatusDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 6.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 6.2: FIND THE ENROLMENT

            Enrolment? existingEnrolment =
                await _context.Enrolments.FirstOrDefaultAsync(
                    item => item.EnrolmentId == id,
                    cancellationToken);

            if (existingEnrolment == null)
            {
                return NotFound(new
                {
                    message = "RaceDay enrolment not found."
                });
            }


            // SECTION 6.3: VERIFY EVENT OWNERSHIP

            // Find the event associated with this enrolment.
            bool ownsEvent = await _context.Events
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.EventId == existingEnrolment.EventId &&
                        item.OrganiserId == organiserId.Value,
                    cancellationToken);

            if (!ownsEvent)
            {
                return Forbid();
            }


            // SECTION 6.4: UPDATE THE STATUS

            // The request DTO ensures the status matches
            // one of the four database-supported values.
            existingEnrolment.Status = request.Status;


            // SECTION 6.5: SAVE TO SQL SERVER

            await _context.SaveChangesAsync(
                cancellationToken);


            // SECTION 6.6: RETRIEVE THE UPDATED RESPONSE

            EnrolmentResponseDto? response =
                await BuildEnrolmentQuery()
                    .FirstOrDefaultAsync(
                        item => item.EnrolmentId == id,
                        cancellationToken);


            // SECTION 6.7: RETURN HTTP 200 OK

            return Ok(response);
        }
    }
}
