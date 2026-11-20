
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models
{
    // Stores the different types of users in RaceDay.
    // The two roles are Organiser and Participant.
    [Table("ROLE", Schema = "dbo")]
    public class Role
    {
        // Unique ID for each role.
        [Key]
        [Column("ROLE_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; }

        // Name of the role assigned to a user.
        [Required]
        [StringLength(30)]
        [Column("ROLE_NAME", TypeName = "varchar(30)")]
        public string RoleName { get; set; } = string.Empty;
    }
}
