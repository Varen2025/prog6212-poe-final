
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    /* ============================================================
       Code Attribution

       TITLE: System.ComponentModel.DataAnnotations Namespace
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for validating category names,
       category types, age ranges and event distances.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: IValidatableObject Interface
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject
       DATE ACCESSED: 19 October 2026

       Usage: Referenced for applying custom validation
       rules to age-based and distance-based categories.

       ============================================================ */

    // Stores the information required when an Organiser
    // creates or updates a RaceDay event category.
    public class CategoryRequestDto : IValidatableObject
    {
        /* ========================================================
           SECTION 1: CATEGORY NAME
           ======================================================== */

        // Every category must have a display name.
        [Required(ErrorMessage = "Category name is required.")]

        // Match the CATEGORY_NAME VARCHAR(100) column.
        [StringLength(100,
            ErrorMessage =
                "Category name cannot exceed 100 characters.")]

        public string CategoryName { get; set; } = string.Empty;


        /* ========================================================
           SECTION 2: CATEGORY TYPE
           ======================================================== */

        // RaceDay supports Age and Distance categories.
        [Required(ErrorMessage = "Category type is required.")]

        // Match the database CHECK constraint.
        [RegularExpression(
            "^(Age|Distance)$",
            ErrorMessage =
                "Category type must be Age or Distance.")]

        public string CategoryType { get; set; } = string.Empty;


        /* ========================================================
           SECTION 3: AGE RANGE
           ======================================================== */

        // Minimum age for an age-based category.
        // Null when the category is distance-based.
        public int? MinAge { get; set; }

        // Maximum age for an age-based category.
        // Null when the category is distance-based.
        public int? MaxAge { get; set; }


        /* ========================================================
           SECTION 4: DISTANCE
           ======================================================== */

        // Distance measured in kilometres.
        // Null when the category is age-based.
        public decimal? DistanceKm { get; set; }


        /* ========================================================
           SECTION 5: CUSTOM CATEGORY VALIDATION
           ======================================================== */

        // Apply additional validation depending on whether
        // the category is based on age or distance.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Reject names containing only spaces.
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                yield return new ValidationResult(
                    "Category name cannot be empty.",
                    new[] { nameof(CategoryName) });
            }


            // SECTION 5.1: VALIDATE AGE CATEGORIES

            if (CategoryType == "Age")
            {
                // Age categories require both age limits.
                if (!MinAge.HasValue || !MaxAge.HasValue)
                {
                    yield return new ValidationResult(
                        "Age categories require minimum and maximum ages.",
                        new[] { nameof(MinAge), nameof(MaxAge) });
                }
                else
                {
                    // Age values cannot be negative.
                    if (MinAge.Value < 0 || MaxAge.Value < 0)
                    {
                        yield return new ValidationResult(
                            "Age values cannot be negative.",
                            new[] { nameof(MinAge), nameof(MaxAge) });
                    }

                    // The minimum age cannot exceed the maximum.
                    if (MinAge.Value > MaxAge.Value)
                    {
                        yield return new ValidationResult(
                            "Minimum age cannot exceed maximum age.",
                            new[] { nameof(MinAge), nameof(MaxAge) });
                    }
                }

                // An Age category should not contain a distance.
                if (DistanceKm.HasValue)
                {
                    yield return new ValidationResult(
                        "Distance must be null for Age categories.",
                        new[] { nameof(DistanceKm) });
                }
            }


            // SECTION 5.2: VALIDATE DISTANCE CATEGORIES

            if (CategoryType == "Distance")
            {
                // Distance categories require a distance.
                if (!DistanceKm.HasValue)
                {
                    yield return new ValidationResult(
                        "Distance is required for Distance categories.",
                        new[] { nameof(DistanceKm) });
                }
                else
                {
                    // Match the DECIMAL(6,2) database range.
                    if (DistanceKm.Value <= 0 ||
                        DistanceKm.Value > 9999.99m)
                    {
                        yield return new ValidationResult(
                            "Distance must be between 0.01 and 9999.99 km.",
                            new[] { nameof(DistanceKm) });
                    }

                    // Reject more than two decimal places.
                    if (decimal.Round(DistanceKm.Value, 2) !=
                        DistanceKm.Value)
                    {
                        yield return new ValidationResult(
                            "Distance cannot have more than two decimal places.",
                            new[] { nameof(DistanceKm) });
                    }
                }

                // Distance categories do not use age limits.
                if (MinAge.HasValue || MaxAge.HasValue)
                {
                    yield return new ValidationResult(
                        "Age limits must be null for Distance categories.",
                        new[] { nameof(MinAge), nameof(MaxAge) });
                }
            }
        }
    }
}
