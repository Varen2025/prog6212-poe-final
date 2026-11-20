/*
   TITLE: Overview of ASP.NET Core MVC
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/aspnet/core/mvc/overview
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for passing an event and its categories to a
   view in one view model.
*/

using System.Collections.Generic;
using RaceDay.Web.Dtos;

namespace RaceDay.Web.Models
{
    // An event together with its categories, for the category list.
    public class CategoryListViewModel
    {
        public EventResponseDto Event { get; set; } = new EventResponseDto();

        public IReadOnlyList<CategoryResponseDto> Categories { get; set; } =
            new List<CategoryResponseDto>();
    }
}