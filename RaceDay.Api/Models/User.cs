
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

       Usage: Referenced for mapping C# model properties
       to the matching SQL Server database columns.
       ============================================================ */

    // Stores the account information of RaceDay users.
    // Each user belongs to either the Organiser or Participant role.
    [Table("USER", Schema = "dbo")]
    public class User
    {
        // Unique identifier for each registered user.
        [Key]
        [Column("USER_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        // Foreign key connecting the user to the ROLE table.
        [Required]
        [Column("ROLE_ID")]
        public int RoleId { get; set; }

        // Stores the user's first name.
        [Required]
        [StringLength(60)]
        [Column("FIRST_NAME", TypeName = "varchar(60)")]
        public string FirstName { get; set; } = string.Empty;

        // Stores the user's surname.
        [Required]
        [StringLength(60)]
        [Column("LAST_NAME", TypeName = "varchar(60)")]
        public string LastName { get; set; } = string.Empty;

        // Stores the email address used during login.
        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Column("EMAIL", TypeName = "varchar(150)")]
        public string Email { get; set; } = string.Empty;

        // Stores the hashed password instead of the original password.
        [Required]
        [StringLength(255)]
        [Column("PASSWORD_HASH", TypeName = "varchar(255)")]
        public string PasswordHash { get; set; } = string.Empty;

        // Stores the user's contact telephone number.
        [Required]
        [StringLength(20)]
        [Column("PHONE_NUMBER", TypeName = "varchar(20)")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Stores the user's date of birth.
        [Required]
        [Column("DATE_OF_BIRTH", TypeName = "date")]
        public DateOnly DateOfBirth { get; set; }

        // Stores the profile picture URL when one is available.
        // This will be used for Azure Blob Storage in Part 3.
        [StringLength(500)]
        [Column("PROFILE_IMAGE_URL", TypeName = "varchar(500)")]
        public string? ProfileImageUrl { get; set; }

        // Records the date and time the account was created.
        // The SQL Server default will be configured in DbContext.
        [Column("CREATED_AT", TypeName = "datetime2(0)")]
        public DateTime CreatedAt { get; set; }

        // Navigation property connecting the user to their role.
        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;
    }
}
