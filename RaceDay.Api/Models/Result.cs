
using System;
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

       Usage: Referenced for mapping the Result model
       properties to their matching SQL Server columns.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: One-to-one Relationships - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-one
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for defining the relationship
       between the Result and Enrolment entities.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: RangeAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for validating the finishing
       position and preventing zero or negative values.

       ============================================================ */

    // Stores the race results of Participants who complete events.
    // Each result belongs to one existing enrolment.
    [Table("RESULT", Schema = "dbo")]
    public class Result
    {
        // Unique identifier for each recorded result.
        [Key]
        [Column("RESULT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResultId { get; set; }

        // Foreign key connecting the result to an enrolment.
        [Column("ENROLMENT_ID")]
        public int EnrolmentId { get; set; }

        // Stores the Participant's recorded finish time.
        // TimeOnly maps to the SQL Server time(0) data type.
        [Column("FINISH_TIME", TypeName = "time(0)")]
        public TimeOnly FinishTime { get; set; }

        // Stores the Participant's finishing position.
        // The position must be greater than zero.
        [Range(1, int.MaxValue,
            ErrorMessage = "Finishing position must be greater than zero.")]
        [Column("FINISHING_POSITION")]
        public int FinishingPosition { get; set; }

        // Indicates whether the result is visible to the Participant.
        // Results are unpublished by default.
        [Column("IS_PUBLISHED")]
        public bool IsPublished { get; set; } = false;

        // Stores the date and time the result was published.
        // Remains null when the result is unpublished.
        [Column("PUBLISHED_AT", TypeName = "datetime2(0)")]
        public DateTime? PublishedAt { get; set; }

        // Navigation property connecting the result to its enrolment.
        [ForeignKey(nameof(EnrolmentId))]
        public Enrolment Enrolment { get; set; } = null!;
    }
}
