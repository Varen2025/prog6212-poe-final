/*
   TITLE: Model validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 19 November 2026

   Usage: Referenced for validating the create and edit event forms
   with DataAnnotations that match the RaceDay API's rules.


   TITLE: Upload files in ASP.NET Core
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for binding the optional banner image file
   on the event forms with IFormFile.
*/

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Web.Models
{
    // The data entered on the create and edit event forms.
    // The rules match the API so most mistakes are caught early.
    public class EventFormViewModel
    {
        // Set when editing. Zero when creating.
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(150,
            ErrorMessage = "The event name can be up to 150 characters.")]
        [Display(Name = "Event name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000,
            ErrorMessage = "The description can be up to 1000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date and start time are required.")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(
            DataFormatString = "{0:yyyy-MM-ddTHH:mm}",
            ApplyFormatInEditMode = true)]
        [Display(Name = "Date and start time")]
        public DateTime? EventDate { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200,
            ErrorMessage = "The location can be up to 200 characters.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Distance is required.")]
        [Range(typeof(decimal), "0.01", "9999.99",
            ErrorMessage = "Distance must be between 0.01 and 9999.99 km.")]
        [Display(Name = "Distance (km)")]
        public decimal? DistanceKm { get; set; }

        [Required(ErrorMessage = "Please choose an event type.")]
        [RegularExpression(
            "^(Run|Walk|Cycle)$",
            ErrorMessage = "Event type must be Run, Walk or Cycle.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = string.Empty;

        // Optional banner picture chosen on the form.
        [Display(Name = "Banner image (optional)")]
        public IFormFile? BannerImage { get; set; }

        // The banner already saved for this event (edit page only).
        // Display only, so it is nullable and never required.
        public string? CurrentBannerUrl { get; set; }
    }
}