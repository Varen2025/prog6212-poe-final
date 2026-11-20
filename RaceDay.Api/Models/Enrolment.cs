
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

       Usage: Referenced for mapping the Enrolment model
       properties to their matching SQL Server columns.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: One-to-many Relationships - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for defining the relationships
       between Enrolment, User, Event and Category.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: RegularExpressionAttribute Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.regularexpressionattribute
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for validating the allowed
       enrolment status values.

       ============================================================ */

    // Stores the details of Participants entering RaceDay events.
    // Every enrolment connects a Participant, Event and Category.
    [Table("ENROLMENT", Schema = "dbo")]
    public class Enrolment
    {
        // Unique identifier for each enrolment.
        [Key]
        [Column("ENROLMENT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EnrolmentId { get; set; }

        // Foreign key identifying the Participant entering the event.
        [Required]
        [Column("PARTICIPANT_ID")]
        public int ParticipantId { get; set; }

        // Foreign key identifying the selected RaceDay event.
        [Required]
        [Column("EVENT_ID")]
        public int EventId { get; set; }

        // Foreign key identifying the selected event category.
        [Required]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        // Stores the date and time the Participant enrolled.
        // The SQL Server default will be configured in DbContext.
        [Required]
        [Column("ENROLMENT_DATE", TypeName = "datetime2(0)")]
        public DateTime EnrolmentDate { get; set; }

        // Stores the current status of the enrolment.
        // A new enrolment starts with the Pending status.
        [Required]
        [StringLength(20)]
        [RegularExpression(
            "^(Pending|Confirmed|Cancelled|Completed)$",
            ErrorMessage =
                "Status must be Pending, Confirmed, Cancelled or Completed.")]
        [Column("STATUS", TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        // Navigation property connecting the enrolment to its Participant.
        [ForeignKey(nameof(ParticipantId))]
        public User Participant { get; set; } = null!;

        // Navigation property connecting the enrolment to its Event.
        [ForeignKey(nameof(EventId))]
        public Event Event { get; set; } = null!;

        // Navigation property connecting the enrolment to its Category.
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;
    }
}
