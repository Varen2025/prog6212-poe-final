
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    /* ============================================================
       Code Attribution

       TITLE: ColumnAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for mapping the Category model
       properties to their SQL Server database columns.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Relationships - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for defining the relationship
       between the Category and Event entities.

       ============================================================ */

    // Stores age-based and distance-based categories.
    // Each category belongs to a specific RaceDay event.
    [Table("CATEGORY", Schema = "dbo")]
    public class Category
    {
        // Unique identifier for each event category.
        [Key]
        [Column("CATEGORY_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryId { get; set; }

        // Foreign key identifying the event this category belongs to.
        [Required]
        [Column("EVENT_ID")]
        public int EventId { get; set; }

        // Stores the category name, such as Under 20 or Open 10km.
        [Required]
        [StringLength(100)]
        [Column("CATEGORY_NAME", TypeName = "varchar(100)")]
        public string CategoryName { get; set; } = string.Empty;

        // Identifies whether the category is Age or Distance.
        [Required]
        [StringLength(20)]
        [RegularExpression("^(Age|Distance)$",
            ErrorMessage = "Category type must be Age or Distance.")]
        [Column("CATEGORY_TYPE", TypeName = "varchar(20)")]
        public string CategoryType { get; set; } = string.Empty;

        // Stores the minimum age for an age-based category.
        // Nullable because distance categories do not require an age.
        [Column("MIN_AGE")]
        public int? MinAge { get; set; }

        // Stores the maximum age for an age-based category.
        // Nullable because distance categories do not require an age.
        [Column("MAX_AGE")]
        public int? MaxAge { get; set; }

        // Stores the distance in kilometres for a distance category.
        // Nullable because age-based categories may not need a distance.
        [Column("DISTANCE_KM", TypeName = "decimal(6,2)")]
        public decimal? DistanceKm { get; set; }

        // Navigation property connecting the category to its event.
        [ForeignKey(nameof(EventId))]
        public Event Event { get; set; } = null!;
    }
}
