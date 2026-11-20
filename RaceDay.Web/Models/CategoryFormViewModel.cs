/*
   TITLE: Model validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for validating the category form with
   DataAnnotations and IValidatableObject so the age and distance
   rules match the RaceDay API.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // The data entered on the create and edit category forms.
    public class CategoryFormViewModel : IValidatableObject
    {
        // The event the category belongs to (from the route).
        public int EventId { get; set; }

        // Set when editing. Zero when creating.
        public int CategoryId { get; set; }

        // Shown on the page only. Nullable so MVC does not treat it
        // as a required form field.
        public string? EventName { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100,
            ErrorMessage = "The category name can be up to 100 characters.")]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a category type.")]
        [RegularExpression(
            "^(Age|Distance)$",
            ErrorMessage = "Category type must be Age or Distance.")]
        [Display(Name = "Category type")]
        public string CategoryType { get; set; } = "Age";

        [Display(Name = "Minimum age")]
        public int? MinAge { get; set; }

        [Display(Name = "Maximum age")]
        public int? MaxAge { get; set; }

        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        // Only the fields that belong to the chosen type are checked.
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (CategoryType == "Age")
            {
                if (!MinAge.HasValue)
                {
                    yield return new ValidationResult(
                        "Minimum age is required.",
                        new[] { nameof(MinAge) });
                }

                if (!MaxAge.HasValue)
                {
                    yield return new ValidationResult(
                        "Maximum age is required.",
                        new[] { nameof(MaxAge) });
                }

                if (MinAge.HasValue && MaxAge.HasValue)
                {
                    if (MinAge.Value < 0 || MaxAge.Value < 0)
                    {
                        yield return new ValidationResult(
                            "Age values cannot be negative.",
                            new[] { nameof(MinAge), nameof(MaxAge) });
                    }
                    else if (MinAge.Value > MaxAge.Value)
                    {
                        yield return new ValidationResult(
                            "Minimum age cannot exceed maximum age.",
                            new[] { nameof(MinAge), nameof(MaxAge) });
                    }
                }
            }

            if (CategoryType == "Distance")
            {
                if (!DistanceKm.HasValue)
                {
                    yield return new ValidationResult(
                        "Distance is required.",
                        new[] { nameof(DistanceKm) });
                }
                else
                {
                    if (DistanceKm.Value <= 0 ||
                        DistanceKm.Value > 9999.99m)
                    {
                        yield return new ValidationResult(
                            "Distance must be between 0.01 and 9999.99 km.",
                            new[] { nameof(DistanceKm) });
                    }
                    else if (decimal.Round(DistanceKm.Value, 2) !=
                        DistanceKm.Value)
                    {
                        yield return new ValidationResult(
                            "Distance cannot have more than two decimal places.",
                            new[] { nameof(DistanceKm) });
                    }
                }
            }
        }
    }
}