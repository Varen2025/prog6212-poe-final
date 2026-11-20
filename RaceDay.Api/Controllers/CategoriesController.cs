
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
using RaceCategory = RaceDay.Api.Models.Category;

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

       Usage: Referenced for creating RESTful category
       endpoints and returning appropriate HTTP responses.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Role-Based Authorization in ASP.NET Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for restricting category management
       to authenticated RaceDay Organisers.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Asynchronous Programming - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/miscellaneous/async
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for querying and saving categories
       asynchronously through Entity Framework Core.

       ============================================================ */

    // Handles public category browsing and Organiser management.
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        // Database context used to access category information.
        private readonly RaceDayDbContext _context;

        // Receive the database context through dependency injection.
        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }


        /* ========================================================
           SECTION 1: CONVERT CATEGORY TO RESPONSE DTO
           ======================================================== */

        // Convert a database category into a response object.
        // Only return information needed by API consumers.
        private static CategoryResponseDto ToResponse(
            RaceCategory category)
        {
            return new CategoryResponseDto
            {
                CategoryId = category.CategoryId,

                EventId = category.EventId,

                CategoryName = category.CategoryName,

                CategoryType = category.CategoryType,

                MinAge = category.MinAge,

                MaxAge = category.MaxAge,

                DistanceKm = category.DistanceKm
            };
        }


        /* ========================================================
           SECTION 2: GET LOGGED-IN USER ID
           ======================================================== */

        // Read the verified user ID from the authentication claims.
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
           SECTION 3: GET CATEGORIES FOR AN EVENT
           GET /api/events/{eventId}/categories
           ======================================================== */

        /// <summary>
        /// Returns all categories belonging to a specified event.
        /// </summary>
        [HttpGet("/api/events/{eventId:int}/categories")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventCategories(
            int eventId,
            CancellationToken cancellationToken)
        {
            // SECTION 3.1: CHECK WHETHER THE EVENT EXISTS

            bool eventExists = await _context.Events
                .AsNoTracking()
                .AnyAsync(
                    item => item.EventId == eventId,
                    cancellationToken);

            // Return 404 if the requested event does not exist.
            if (!eventExists)
            {
                return NotFound(new
                {
                    message = "RaceDay event not found."
                });
            }


            // SECTION 3.2: GET THE EVENT'S CATEGORIES

            // Retrieve only categories belonging to this event.
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(category => category.EventId == eventId)
                .OrderBy(category => category.CategoryName)
                .ToListAsync(cancellationToken);


            // SECTION 3.3: RETURN THE CATEGORY LIST

            var response = categories
                .Select(ToResponse)
                .ToList();

            return Ok(response);
        }


        /* ========================================================
           SECTION 4: GET CATEGORY BY ID
           GET /api/categories/{id}
           ======================================================== */

        /// <summary>
        /// Returns information for one RaceDay category.
        /// </summary>
        [HttpGet("/api/categories/{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(CategoryResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoryById(
            int id,
            CancellationToken cancellationToken)
        {
            // SECTION 4.1: FIND THE CATEGORY

            RaceCategory? category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.CategoryId == id,
                    cancellationToken);


            // SECTION 4.2: HANDLE A MISSING CATEGORY

            if (category == null)
            {
                return NotFound(new
                {
                    message = "RaceDay category not found."
                });
            }


            // SECTION 4.3: RETURN HTTP 200 OK

            return Ok(ToResponse(category));
        }


        /* ========================================================
           SECTION 5: CREATE AN EVENT CATEGORY
           POST /api/events/{eventId}/categories
           ======================================================== */

        /// <summary>
        /// Creates a category for an event owned by the Organiser.
        /// </summary>
        [HttpPost("/api/events/{eventId:int}/categories")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(CategoryResponseDto),
            StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            [FromBody] CategoryRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 5.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 5.2: FIND THE SELECTED EVENT

            var selectedEvent = await _context.Events
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

            // An Organiser can only add categories to their own events.
            if (selectedEvent.OrganiserId != organiserId.Value)
            {
                return Forbid();
            }


            // SECTION 5.4: CHECK FOR DUPLICATE CATEGORY NAMES

            // Remove surrounding spaces before checking.
            string categoryName = request.CategoryName.Trim();

            // A category name must be unique within its event.
            bool categoryExists = await _context.Categories
                .AnyAsync(
                    category =>
                        category.EventId == eventId &&
                        category.CategoryName == categoryName,
                    cancellationToken);

            if (categoryExists)
            {
                return Conflict(new
                {
                    message =
                        "A category with this name already exists " +
                        "for the selected event."
                });
            }


            // SECTION 5.5: CREATE THE CATEGORY ENTITY

            RaceCategory newCategory = new RaceCategory
            {
                // Link the category to the selected event.
                EventId = eventId,

                // Store the category's display name.
                CategoryName = categoryName,

                // Store whether this is an Age or Distance category.
                CategoryType = request.CategoryType,

                // Age values are nullable for Distance categories.
                MinAge = request.MinAge,

                MaxAge = request.MaxAge,

                // Distance is nullable for Age categories.
                DistanceKm = request.DistanceKm
            };


            // SECTION 5.6: SAVE TO SQL SERVER

            _context.Categories.Add(newCategory);

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
                // Handle a uniqueness conflict occurring during save.
                return Conflict(new
                {
                    message =
                        "A category with this name already exists " +
                        "for the selected event."
                });
            }


            // SECTION 5.7: RETURN HTTP 201 CREATED

            // Include a link to the newly created category.
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = newCategory.CategoryId },
                ToResponse(newCategory));
        }


        /* ========================================================
           SECTION 6: UPDATE AN EXISTING CATEGORY
           PUT /api/categories/{id}
           ======================================================== */

        /// <summary>
        /// Updates a category belonging to the Organiser's event.
        /// </summary>
        [HttpPut("/api/categories/{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(
            typeof(CategoryResponseDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryRequestDto request,
            CancellationToken cancellationToken)
        {
            // SECTION 6.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 6.2: FIND THE CATEGORY

            RaceCategory? existingCategory =
                await _context.Categories.FirstOrDefaultAsync(
                    category => category.CategoryId == id,
                    cancellationToken);

            if (existingCategory == null)
            {
                return NotFound(new
                {
                    message = "RaceDay category not found."
                });
            }


            // SECTION 6.3: VERIFY EVENT OWNERSHIP

            // Check that the category belongs to an event
            // managed by the currently logged-in Organiser.
            bool ownsEvent = await _context.Events
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.EventId == existingCategory.EventId &&
                        item.OrganiserId == organiserId.Value,
                    cancellationToken);

            if (!ownsEvent)
            {
                return Forbid();
            }


            // SECTION 6.4: CHECK FOR DUPLICATE NAMES

            string categoryName = request.CategoryName.Trim();

            // Ignore the current category when looking for duplicates.
            bool duplicateExists = await _context.Categories
                .AnyAsync(
                    category =>
                        category.EventId == existingCategory.EventId &&
                        category.CategoryId != id &&
                        category.CategoryName == categoryName,
                    cancellationToken);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message =
                        "A category with this name already exists " +
                        "for the selected event."
                });
            }


            // SECTION 6.5: UPDATE ALLOWED CATEGORY FIELDS

            existingCategory.CategoryName = categoryName;

            existingCategory.CategoryType = request.CategoryType;

            existingCategory.MinAge = request.MinAge;

            existingCategory.MaxAge = request.MaxAge;

            existingCategory.DistanceKm = request.DistanceKm;

            // Do not modify CategoryId or EventId.


            // SECTION 6.6: SAVE UPDATED CATEGORY

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
                return Conflict(new
                {
                    message =
                        "A category with this name already exists " +
                        "for the selected event."
                });
            }


            // SECTION 6.7: RETURN UPDATED CATEGORY

            return Ok(ToResponse(existingCategory));
        }


        /* ========================================================
           SECTION 7: DELETE AN EVENT CATEGORY
           DELETE /api/categories/{id}
           ======================================================== */

        /// <summary>
        /// Deletes a category that is not used by an enrolment.
        /// </summary>
        [HttpDelete("/api/categories/{id:int}")]
        [Authorize(Policy = "OrganiserOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(
            int id,
            CancellationToken cancellationToken)
        {
            // SECTION 7.1: IDENTIFY THE ORGANISER

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
            {
                return Unauthorized();
            }


            // SECTION 7.2: FIND THE CATEGORY

            RaceCategory? existingCategory =
                await _context.Categories.FirstOrDefaultAsync(
                    category => category.CategoryId == id,
                    cancellationToken);

            if (existingCategory == null)
            {
                return NotFound(new
                {
                    message = "RaceDay category not found."
                });
            }


            // SECTION 7.3: VERIFY EVENT OWNERSHIP

            // Only the Organiser who owns the event may delete
            // a category belonging to that event.
            bool ownsEvent = await _context.Events
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.EventId == existingCategory.EventId &&
                        item.OrganiserId == organiserId.Value,
                    cancellationToken);

            if (!ownsEvent)
            {
                return Forbid();
            }


            // SECTION 7.4: CHECK FOR RELATED ENROLMENTS

            // Participants may already be using this category.
            bool categoryInUse = await _context.Enrolments
                .AnyAsync(
                    enrolment => enrolment.CategoryId == id,
                    cancellationToken);

            // Do not delete a category used by a Participant.
            if (categoryInUse)
            {
                return Conflict(new
                {
                    message =
                        "This category cannot be deleted because " +
                        "it is being used by an enrolment."
                });
            }


            // SECTION 7.5: REMOVE THE CATEGORY

            _context.Categories.Remove(existingCategory);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException
                    sqlException && sqlException.Number == 547)
            {
                // SQL Server may reject deletion if another
                // enrolment was created before the save.
                return Conflict(new
                {
                    message =
                        "This category has related records " +
                        "and cannot be deleted."
                });
            }


            // SECTION 7.6: RETURN HTTP 204 NO CONTENT

            return NoContent();
        }
    }
}
