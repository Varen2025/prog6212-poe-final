
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

       Usage: Referenced for mapping the Event model properties
       to their corresponding SQL Server columns.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: RangeAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for validating the event distance
       and preventing zero or negative distance values.

       ============================================================ */

    // Stores running, walking and cycling events in RaceDay.
    // Every event is created and managed by an Organiser.
    [Table("EVENT", Schema = "dbo")]
    public class Event
    {
        // Unique identifier for each RaceDay event.
        [Key]
        [Column("EVENT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        // Foreign key identifying the Organiser who created the event.
        [Required]
        [Column("ORGANISER_ID")]
        public int OrganiserId { get; set; }

        // Stores the name of the event.
        [Required]
        [StringLength(150)]
        [Column("NAME", TypeName = "varchar(150)")]
        public string Name { get; set; } = string.Empty;

        // Stores information describing the event.
        [Required]
        [StringLength(1000)]
        [Column("DESCRIPTION", TypeName = "varchar(1000)")]
        public string Description { get; set; } = string.Empty;

        // Stores the date and starting time of the event.
        [Required]
        [Column("EVENT_DATE", TypeName = "datetime2(0)")]
        public DateTime EventDate { get; set; }

        // Stores the location where the event takes place.
        [Required]
        [StringLength(200)]
        [Column("LOCATION", TypeName = "varchar(200)")]
        public string Location { get; set; } = string.Empty;

        // Stores the event distance in kilometres.
        // The distance must be greater than zero.
        [Required]
        [Range(typeof(decimal), "0.01", "9999.99")]
        [Column("DISTANCE_KM", TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        // Identifies whether the event is a Run, Walk or Cycle.
        [Required]
        [StringLength(20)]
        [RegularExpression("^(Run|Walk|Cycle)$",
            ErrorMessage = "Event type must be Run, Walk or Cycle.")]
        [Column("EVENT_TYPE", TypeName = "varchar(20)")]
        public string EventType { get; set; } = string.Empty;

        // Stores the event banner image URL when available.
        // Azure Blob Storage integration will be added in Part 3.
        [StringLength(500)]
        [Column("BANNER_IMAGE_URL", TypeName = "varchar(500)")]
        public string? BannerImageUrl { get; set; }

        // Records when the event was created.
        // The SQL Server default will be configured in DbContext.
        [Column("CREATED_AT", TypeName = "datetime2(0)")]
        public DateTime CreatedAt { get; set; }

        // Navigation property connecting the Event to its Organiser.
        [ForeignKey(nameof(OrganiserId))]
        public User Organiser { get; set; } = null!;
    }
}
