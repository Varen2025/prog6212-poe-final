
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
using RaceDay.Api.Services;
using RaceEvent = RaceDay.Api.Models.Event;

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

       Usage: Referenced for implementing RESTful event
       endpoints and returning HTTP response codes.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting event management
       operations to authenticated Organisers.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for asynchronous database queries
       and saving event information to SQL Server.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Upload files in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
       DATE ACCESSED: 20 November 2026

       Usage: Referenced for receiving a multipart image upload
       and storing it through the blob storage service.

       ============================================================ */

    // Manages running, walking and cycling events.
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        // Database context for accessing RaceDay events.
        private readonly RaceDayDbContext _context;

        // Stores uploaded banner images.
        private readonly IBlobStorageService _blobStorage;

        // Receive services through dependency injection.
        public EventsController(
            RaceDayDbContext context,
            IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }


        /* ========================================================
           SECTION 1: CONVERT EVENT TO RESPONSE DTO
           ======================================================== */

        // Convert an Event entity into a response DTO.
        // This prevents database entities and navigation
        // properties from being returned directly.
        private static EventResponseDto ToResponse(RaceEvent item)
        {
            return new EventResponseDto
            {
                EventId = item.EventId,
                OrganiserId = item.OrganiserId,
                Name = item.Name,
                Description = item.Description,
                EventDate = item.EventDate,
                Location = item.Location,
                DistanceKm = item.DistanceKm,
                EventType = item.EventType,
                BannerImageUrl = item.BannerImageUrl,
                CreatedAt = item.CreatedAt
            };
        }


        /* ========================================================
           SECTION 2: GET AUTHENTICATED USER ID
           ======================================================== */

        // Read the ID supplied by our session authentication
        // handler rather than accepting an ID from the browser.
        private int? GetCurrentUserId()
        {
            string? userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdValue, out int userId) &&
                userId > 0)
            {
                return userId;
            }

            return null;
        }


        /* ========================================================
           SECTION 3: GET UPCOMING EVENTS
           GET /api/events
           ======================================================== */

        /// <summary>
        /// Returns upcoming RaceDay events.
        /// </summary>
        /// <remarks>
        /// Public endpoint supporting optional event type,
        /// location and date filters.
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? eventType,
            [FromQuery] string? location,
            [FromQuery] DateOnly? date,
            CancellationToken cancellationToken)
        {
            // SECTION 3.1: START THE DATABASE QUERY

            // Only show upcoming events in the public listing.
            DateTime now = DateTime.Now;

            IQueryable<RaceEvent> query = _context.Events
                .AsNoTracking()
                .Where(item => item.EventDate >= now);


            // SECTION 3.2: FILTER BY EVENT TYPE

            // Validate and normalise the optional event type.
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                string? selectedType =
                    eventType.Trim().ToLowerInvariant() switch
                    {
                        "run" => "Run",
                        "walk" => "Walk",
                        "cycle" => "Cycle",
                        _ => null
                    };

                // Reject unsupported event types.
                if (selectedType == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Event type must be Run, Walk or Cycle."
                    });
                }

                query = query.Where(item =>
                    item.EventType == selectedType);
            }


            // SECTION 3.3: FILTER BY LOCATION

            // Search for matching locations if requested.
            if (!string.IsNullOrWhiteSpace(location))
            {
                string selectedLocation =
                    location.Trim().ToLowerInvariant();

                query = query.Where(item =>
                    item.Location.ToLower()
                        .Contains(selectedLocation));
            }


            // SECTION 3.4: FILTER BY EVENT DATE

            // Match events taking place on the selected day.
            if (date.HasValue)
            {
                DateTime startDate =
                    date.Value.ToDateTime(TimeOnly.MinValue);

                DateTime endDate = startDate.AddDays(1);

                query = query.Where(item =>
                    item.EventDate >= startDate &&
                    item.EventDate < endDate);
            }


            // SECTION 3.5: EXECUTE THE QUERY

            // Sort events by their scheduled dates.
            var events = await query
                .OrderBy(item => item.EventDate)
                .ToListAsync(cancellationToken);


            // SECTION 3.6: PREPARE AND RETURN THE RESPONSE

            var response = events
                .Select(ToResponse)
                .ToList();

            return Ok(response);
        }


        /* ========================================================
           SECTION 4: GET EVENT BY ID
           GET /api/events/{id}
           ======================================================== */

        /// <summary>
        /// Returns information about a specific RaceDay event.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(EventResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventById(
            int id,
            CancellationToken cancellationToken)
        {
            // SECTION 4.1: FIND THE EVENT

            RaceEvent? existingEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.EventId == id,
                    cancellationToken);


            // SECTION 4.2: HANDLE A MISSING EVENT

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 4.3: RETURN THE EVENT

            return Ok(ToResponse(existingEvent));
        }


        /* ========================================================
           SECTION 5: GET ORGANISER'S EVENTS
           GET /api/events/mine
           ======================================================== */

        /// <summary>
        /// Returns events created by the logged-in Organiser.
        /// </summary>
        [HttpGet("mine")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEvents(
            CancellationToken cancellationToken)
        {
            // SECTION 5.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 5.2: FIND THE ORGANISER'S EVENTS

            // Retrieve only events owned by this Organiser.
            var events = await _context.Events
                .AsNoTracking()
                .Where(item =>
                    item.OrganiserId == organiserId.Value)
                .OrderBy(item => item.EventDate)
                .ToListAsync(cancellationToken);


            // SECTION 5.3: RETURN THE EVENT LIST

            var response = events
                .Select(ToResponse)
                .ToList();

            return Ok(response);
        }


        /* ========================================================
           SECTION 6: CREATE A NEW EVENT
           POST /api/events
           ======================================================== */

        /// <summary>
        /// Creates a new RaceDay event.
        /// </summary>
        /// <remarks>
        /// Only a logged-in Organiser can create events.
        /// The Organiser ID is taken from the current session.
        /// </remarks>
        [HttpPost]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(EventResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent(
            [FromBody] EventRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 6.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 6.2: CREATE THE EVENT ENTITY

            // The authenticated Organiser becomes the owner.
            RaceEvent newEvent = new RaceEvent
            {
                OrganiserId = organiserId.Value,

                Name = request.Name.Trim(),

                Description = request.Description.Trim(),

                EventDate = request.EventDate,

                Location = request.Location.Trim(),

                DistanceKm = request.DistanceKm,

                EventType = request.EventType,

                // Banner images are uploaded separately
                // using POST /api/events/{id}/banner.
                BannerImageUrl = null
            };


            // SECTION 6.3: SAVE TO SQL SERVER

            // Add the new event to Entity Framework Core.
            _context.Events.Add(newEvent);

            // Save the record and retrieve generated values.
            await _context.SaveChangesAsync(
                cancellationToken);


            // SECTION 6.4: RETURN HTTP 201 CREATED

            // The response contains the new event and
            // a Location header pointing to its GET endpoint.
            return CreatedAtAction(
                nameof(GetEventById),
                new { id = newEvent.EventId },
                ToResponse(newEvent));
        }


        /* ========================================================
           SECTION 7: UPDATE AN EXISTING EVENT
           PUT /api/events/{id}
           ======================================================== */

        /// <summary>
        /// Updates an event owned by the logged-in Organiser.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(EventResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] EventRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 7.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 7.2: FIND THE EVENT

            RaceEvent? existingEvent =
                await _context.Events.FirstOrDefaultAsync(
                    item => item.EventId == id,
                    cancellationToken);


            // SECTION 7.3: CHECK WHETHER EVENT EXISTS

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 7.4: VERIFY EVENT OWNERSHIP

            // An Organiser may only edit their own events.
            if (existingEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 7.5: UPDATE ALLOWED EVENT DETAILS

            existingEvent.Name = request.Name.Trim();

            existingEvent.Description =
                request.Description.Trim();

            existingEvent.EventDate = request.EventDate;

            existingEvent.Location = request.Location.Trim();

            existingEvent.DistanceKm = request.DistanceKm;

            existingEvent.EventType = request.EventType;

            // Do not change the EventId, OrganiserId,
            // CreatedAt or BannerImageUrl.


            // SECTION 7.6: SAVE UPDATED INFORMATION

            await _context.SaveChangesAsync(
                cancellationToken);


            // SECTION 7.7: RETURN HTTP 200 OK

            return Ok(ToResponse(existingEvent));
        }


        /* ========================================================
           SECTION 8: DELETE AN EVENT
           DELETE /api/events/{id}
           ======================================================== */

        /// <summary>
        /// Deletes an event owned by the logged-in Organiser.
        /// </summary>
        /// <remarks>
        /// Deletion is rejected when related categories or
        /// enrolments exist, protecting database integrity.
        /// </remarks>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(
            int id,
            CancellationToken cancellationToken)
        {
            // SECTION 8.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 8.2: FIND THE EVENT

            RaceEvent? existingEvent =
                await _context.Events.FirstOrDefaultAsync(
                    item => item.EventId == id,
                    cancellationToken);


            // SECTION 8.3: CHECK WHETHER EVENT EXISTS

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 8.4: VERIFY EVENT OWNERSHIP

            // Reject an Organiser who does not own the event.
            if (existingEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 8.5: CHECK RELATED DATABASE RECORDS

            // Check whether categories already belong to the event.
            bool hasCategories = await _context.Categories
                .AnyAsync(
                    category => category.EventId == id,
                    cancellationToken);

            // Check whether Participants have enrolled.
            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(
                    enrolment => enrolment.EventId == id,
                    cancellationToken);

            // Prevent deletion when related records exist.
            if (hasCategories || hasEnrolments)
            {
                return Conflict(new
                {
                    message =
                        "This event cannot be deleted because " +
                        "related categories or enrolments exist."
                });
            }


            // SECTION 8.6: REMOVE THE EVENT

            _context.Events.Remove(existingEvent);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException
                    sqlException && sqlException.Number == 547)
            {
                // A foreign key may have been created between
                // the related-record checks and the delete.
                return Conflict(new
                {
                    message =
                        "This event has related records and " +
                        "cannot be deleted."
                });
            }


            // SECTION 8.7: RETURN HTTP 204 NO CONTENT

            return NoContent();
        }


        /* ========================================================
           SECTION 9: UPLOAD EVENT BANNER
           POST /api/events/{id}/banner
           ======================================================== */

        /// <summary>
        /// Uploads a banner image for an event owned by the Organiser.
        /// </summary>
        /// <remarks>
        /// Accepts JPG, PNG or WebP up to 2 MB. The image is stored
        /// in Azure Blob Storage and its URL is saved on the event.
        /// </remarks>
        [HttpPost("{id:int}/banner")]
        [Authorize(Policy = "OrganiserOnly")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(
            typeof(EventResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UploadBanner(
            int id,
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            // SECTION 9.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 9.2: FIND THE EVENT AND CHECK OWNERSHIP

            RaceEvent? existingEvent =
                await _context.Events.FirstOrDefaultAsync(
                    item => item.EventId == id,
                    cancellationToken);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }

            if (existingEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 9.3: VALIDATE THE IMAGE

            ImageCheckResult check =
                await ImageUploadValidator.ValidateAsync(
                    file,
                    cancellationToken);

            if (!check.IsValid)
            {
                return BadRequest(new { message = check.Error });
            }


            // SECTION 9.4: UPLOAD TO AZURE AND SAVE THE URL

            try
            {
                await using var stream = file!.OpenReadStream();

                existingEvent.BannerImageUrl =
                    await _blobStorage.UploadImageAsync(
                        stream,
                        check.ContentType,
                        "banners",
                        check.Extension,
                        cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message =
                            "Image storage is currently unavailable."
                    });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(ToResponse(existingEvent));
        }
    }
}