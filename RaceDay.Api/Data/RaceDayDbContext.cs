
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    /* ============================================================
       Code Attribution

       TITLE: DbContext Class
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for creating the RaceDay database
       context and registering the entity models.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: One-to-many Relationships - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for configuring foreign keys
       and relationships between RaceDay entities.

       ============================================================ */

    /* ============================================================
       Code Attribution

       TITLE: Generated Values - EF Core
       AUTHOR: Microsoft
       DATE: n.d.
       AVAILABLE:
       https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties
       DATE ACCESSED: 17 October 2026

       Usage: Referenced for configuring SQL Server
       default values and named default constraints.

       ============================================================ */

    // Manages the connection between the RaceDay models
    // and the SQL Server database using Entity Framework Core.
    public class RaceDayDbContext : DbContext
    {
        // Passes database configuration settings to DbContext.
        public RaceDayDbContext(
            DbContextOptions<RaceDayDbContext> options)
            : base(options)
        {
        }

        /* ============================================================
           SECTION 1: REGISTER DATABASE TABLES
           ============================================================ */

        // Stores the two RaceDay application roles.
        public DbSet<Role> Roles => Set<Role>();

        // Stores Organiser and Participant account information.
        public DbSet<User> Users => Set<User>();

        // Stores running, walking and cycling events.
        public DbSet<Event> Events => Set<Event>();

        // Stores age-based and distance-based event categories.
        public DbSet<Category> Categories => Set<Category>();

        // Stores Participants' event enrolments.
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();

        // Stores the recorded results for completed events.
        public DbSet<Result> Results => Set<Result>();

        /* ============================================================
           SECTION 2: CONFIGURE DATABASE RELATIONSHIPS
           ============================================================ */

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            /* ========================================================
               SECTION 2.1: ROLE TABLE
               ======================================================== */

            modelBuilder.Entity<Role>(entity =>
            {
                // Match the ROLE table from Part 1.
                entity.ToTable("ROLE", "dbo");

                // Configure the primary key.
                entity.HasKey(role => role.RoleId)
                    .HasName("PK_ROLE");

                // Prevent duplicate role names.
                entity.HasAlternateKey(role => role.RoleName)
                    .HasName("UQ_ROLE_ROLE_NAME");

                // Add the two roles required by RaceDay.
                // These records will be created during migration.
                entity.HasData(
                    new Role
                    {
                        RoleId = 1,
                        RoleName = "Organiser"
                    },
                    new Role
                    {
                        RoleId = 2,
                        RoleName = "Participant"
                    }
                );
            });


            /* ========================================================
               SECTION 2.2: USER TABLE
               ======================================================== */

            modelBuilder.Entity<User>(entity =>
            {
                // Match the USER table from Part 1.
                entity.ToTable("USER", "dbo");

                // Configure the primary key.
                entity.HasKey(user => user.UserId)
                    .HasName("PK_USER");

                // Prevent duplicate email addresses.
                entity.HasAlternateKey(user => user.Email)
                    .HasName("UQ_USER_EMAIL");

                // Use SQL Server to generate the creation timestamp.
                entity.Property(user => user.CreatedAt)
                    .HasDefaultValueSql(
                        "SYSDATETIME()",
                        "DF_USER_CREATED_AT");

                // Each User must belong to an existing Role.
                entity.HasOne(user => user.Role)
                    .WithMany()
                    .HasForeignKey(user => user.RoleId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_USER_ROLE");
            });

            /* ========================================================
               SECTION 2.3: EVENT TABLE
               ======================================================== */

            modelBuilder.Entity<Event>(entity =>
            {
                // Match the EVENT table and its check constraints.
                entity.ToTable("EVENT", "dbo", table =>
                {
                    // Event distance must be greater than zero.
                    table.HasCheckConstraint(
                        "CK_EVENT_DISTANCE",
                        "[DISTANCE_KM] > 0");

                    // Only the three RaceDay event types are valid.
                    table.HasCheckConstraint(
                        "CK_EVENT_TYPE",
                        "[EVENT_TYPE] IN ('Run', 'Walk', 'Cycle')");
                });

                // Configure the primary key.
                entity.HasKey(raceEvent => raceEvent.EventId)
                    .HasName("PK_EVENT");

                // Automatically record when an event was created.
                entity.Property(raceEvent => raceEvent.CreatedAt)
                    .HasDefaultValueSql(
                        "SYSDATETIME()",
                        "DF_EVENT_CREATED_AT");

                // Connect each Event to its Organiser.
                entity.HasOne(raceEvent => raceEvent.Organiser)
                    .WithMany()
                    .HasForeignKey(raceEvent => raceEvent.OrganiserId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_EVENT_ORGANISER");
            });

            /* ========================================================
               SECTION 2.4: CATEGORY TABLE
               ======================================================== */

            modelBuilder.Entity<Category>(entity =>
            {
                // Match the CATEGORY table and its check constraints.
                entity.ToTable("CATEGORY", "dbo", table =>
                {
                    // Allow only Age and Distance categories.
                    table.HasCheckConstraint(
                        "CK_CATEGORY_TYPE",
                        "[CATEGORY_TYPE] IN ('Age', 'Distance')");

                    // Prevent invalid minimum and maximum ages.
                    table.HasCheckConstraint(
                        "CK_CATEGORY_AGE_RANGE",
                        "[MIN_AGE] IS NULL OR " +
                        "[MAX_AGE] IS NULL OR " +
                        "[MIN_AGE] <= [MAX_AGE]");

                    // Distance must be positive when provided.
                    table.HasCheckConstraint(
                        "CK_CATEGORY_DISTANCE",
                        "[DISTANCE_KM] IS NULL OR " +
                        "[DISTANCE_KM] > 0");
                });

                // Configure the primary key.
                entity.HasKey(category => category.CategoryId)
                    .HasName("PK_CATEGORY");

                // Prevent duplicate category names in the same event.
                // A unique index permits category names to be edited.
                entity.HasIndex(category => new
                {
                    category.EventId,
                    category.CategoryName
                })
                    .IsUnique()
                    .HasDatabaseName("UQ_CATEGORY_EVENT_NAME");

                // Connect each Category to its Event.
                entity.HasOne(category => category.Event)
                    .WithMany()
                    .HasForeignKey(category => category.EventId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_CATEGORY_EVENT");
            });

            /* ========================================================
               SECTION 2.5: ENROLMENT TABLE
               ======================================================== */

            modelBuilder.Entity<Enrolment>(entity =>
            {
                // Match the ENROLMENT table.
                entity.ToTable("ENROLMENT", "dbo", table =>
                {
                    // Restrict enrolment statuses to valid values.
                    table.HasCheckConstraint(
                        "CK_ENROLMENT_STATUS",
                        "[STATUS] IN " +
                        "('Pending', 'Confirmed', " +
                        "'Cancelled', 'Completed')");
                });

                // Configure the primary key.
                entity.HasKey(enrolment => enrolment.EnrolmentId)
                    .HasName("PK_ENROLMENT");

                // Prevent duplicate enrolments for the same event.
                entity.HasAlternateKey(enrolment => new
                {
                    enrolment.ParticipantId,
                    enrolment.EventId
                })
                    .HasName("UQ_ENROLMENT_PARTICIPANT_EVENT");

                // Automatically record the enrolment date.
                entity.Property(enrolment => enrolment.EnrolmentDate)
                    .HasDefaultValueSql(
                        "SYSDATETIME()",
                        "DF_ENROLMENT_DATE");

                // New enrolments start with Pending status.
                entity.Property(enrolment => enrolment.Status)
                    .HasDefaultValue(
                        "Pending",
                        "DF_ENROLMENT_STATUS");

                // Connect the Enrolment to its Participant.
                entity.HasOne(enrolment => enrolment.Participant)
                    .WithMany()
                    .HasForeignKey(enrolment => enrolment.ParticipantId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_ENROLMENT_PARTICIPANT");

                // Connect the Enrolment to its Event.
                entity.HasOne(enrolment => enrolment.Event)
                    .WithMany()
                    .HasForeignKey(enrolment => enrolment.EventId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_ENROLMENT_EVENT");

                // Connect the Enrolment to its Category.
                entity.HasOne(enrolment => enrolment.Category)
                    .WithMany()
                    .HasForeignKey(enrolment => enrolment.CategoryId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_ENROLMENT_CATEGORY");
            });

            /* ========================================================
               SECTION 2.6: RESULT TABLE
               ======================================================== */

            modelBuilder.Entity<Result>(entity =>
            {
                // Match the RESULT table and its check constraints.
                entity.ToTable("RESULT", "dbo", table =>
                {
                    // Finishing positions must be greater than zero.
                    table.HasCheckConstraint(
                        "CK_RESULT_POSITION",
                        "[FINISHING_POSITION] > 0");

                    // Published results require a publication date.
                    // Unpublished results must not have one.
                    table.HasCheckConstraint(
                        "CK_RESULT_PUBLISHED",
                        "([IS_PUBLISHED] = 0 AND " +
                        "[PUBLISHED_AT] IS NULL) OR " +
                        "([IS_PUBLISHED] = 1 AND " +
                        "[PUBLISHED_AT] IS NOT NULL)");
                });

                // Configure the primary key.
                entity.HasKey(result => result.ResultId)
                    .HasName("PK_RESULT");

                // Prevent more than one Result per Enrolment.
                entity.HasAlternateKey(result => result.EnrolmentId)
                    .HasName("UQ_RESULT_ENROLMENT");

                // Results are unpublished by default.
                entity.Property(result => result.IsPublished)
                    .HasDefaultValue(
                        false,
                        "DF_RESULT_IS_PUBLISHED");

                // Configure the optional one-to-one relationship.
                entity.HasOne(result => result.Enrolment)
                    .WithOne()
                    .HasForeignKey<Result>(
                        result => result.EnrolmentId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_RESULT_ENROLMENT");
            });
        }
    }
}
