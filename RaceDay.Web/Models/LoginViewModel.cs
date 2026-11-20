/*
   TITLE: Model validation in ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
   DATE ACCESSED: 17 November 2026

   Usage: Referenced for validating the login form with
   DataAnnotations before calling the RaceDay API.
*/

using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // The data entered on the login form.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}