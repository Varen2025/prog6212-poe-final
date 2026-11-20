using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "ROLE",
                schema: "dbo",
                columns: table => new
                {
                    ROLE_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROLE_NAME = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROLE", x => x.ROLE_ID);
                    table.UniqueConstraint("UQ_ROLE_ROLE_NAME", x => x.ROLE_NAME);
                });

            migrationBuilder.CreateTable(
                name: "USER",
                schema: "dbo",
                columns: table => new
                {
                    USER_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ROLE_ID = table.Column<int>(type: "int", nullable: false),
                    FIRST_NAME = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    LAST_NAME = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    EMAIL = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    PASSWORD_HASH = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PHONE_NUMBER = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DATE_OF_BIRTH = table.Column<DateOnly>(type: "date", nullable: false),
                    PROFILE_IMAGE_URL = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_USER_CREATED_AT")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER", x => x.USER_ID);
                    table.UniqueConstraint("UQ_USER_EMAIL", x => x.EMAIL);
                    table.ForeignKey(
                        name: "FK_USER_ROLE",
                        column: x => x.ROLE_ID,
                        principalSchema: "dbo",
                        principalTable: "ROLE",
                        principalColumn: "ROLE_ID");
                });

            migrationBuilder.CreateTable(
                name: "EVENT",
                schema: "dbo",
                columns: table => new
                {
                    EVENT_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ORGANISER_ID = table.Column<int>(type: "int", nullable: false),
                    NAME = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    EVENT_DATE = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    LOCATION = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DISTANCE_KM = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EVENT_TYPE = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    BANNER_IMAGE_URL = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_EVENT_CREATED_AT")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVENT", x => x.EVENT_ID);
                    table.CheckConstraint("CK_EVENT_DISTANCE", "[DISTANCE_KM] > 0");
                    table.CheckConstraint("CK_EVENT_TYPE", "[EVENT_TYPE] IN ('Run', 'Walk', 'Cycle')");
                    table.ForeignKey(
                        name: "FK_EVENT_ORGANISER",
                        column: x => x.ORGANISER_ID,
                        principalSchema: "dbo",
                        principalTable: "USER",
                        principalColumn: "USER_ID");
                });

            migrationBuilder.CreateTable(
                name: "CATEGORY",
                schema: "dbo",
                columns: table => new
                {
                    CATEGORY_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EVENT_ID = table.Column<int>(type: "int", nullable: false),
                    CATEGORY_NAME = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CATEGORY_TYPE = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    MIN_AGE = table.Column<int>(type: "int", nullable: true),
                    MAX_AGE = table.Column<int>(type: "int", nullable: true),
                    DISTANCE_KM = table.Column<decimal>(type: "decimal(6,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORY", x => x.CATEGORY_ID);
                    table.CheckConstraint("CK_CATEGORY_AGE_RANGE", "[MIN_AGE] IS NULL OR [MAX_AGE] IS NULL OR [MIN_AGE] <= [MAX_AGE]");
                    table.CheckConstraint("CK_CATEGORY_DISTANCE", "[DISTANCE_KM] IS NULL OR [DISTANCE_KM] > 0");
                    table.CheckConstraint("CK_CATEGORY_TYPE", "[CATEGORY_TYPE] IN ('Age', 'Distance')");
                    table.ForeignKey(
                        name: "FK_CATEGORY_EVENT",
                        column: x => x.EVENT_ID,
                        principalSchema: "dbo",
                        principalTable: "EVENT",
                        principalColumn: "EVENT_ID");
                });

            migrationBuilder.CreateTable(
                name: "ENROLMENT",
                schema: "dbo",
                columns: table => new
                {
                    ENROLMENT_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PARTICIPANT_ID = table.Column<int>(type: "int", nullable: false),
                    EVENT_ID = table.Column<int>(type: "int", nullable: false),
                    CATEGORY_ID = table.Column<int>(type: "int", nullable: false),
                    ENROLMENT_DATE = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_ENROLMENT_DATE"),
                    STATUS = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                        .Annotation("Relational:DefaultConstraintName", "DF_ENROLMENT_STATUS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENROLMENT", x => x.ENROLMENT_ID);
                    table.UniqueConstraint("UQ_ENROLMENT_PARTICIPANT_EVENT", x => new { x.PARTICIPANT_ID, x.EVENT_ID });
                    table.CheckConstraint("CK_ENROLMENT_STATUS", "[STATUS] IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')");
                    table.ForeignKey(
                        name: "FK_ENROLMENT_CATEGORY",
                        column: x => x.CATEGORY_ID,
                        principalSchema: "dbo",
                        principalTable: "CATEGORY",
                        principalColumn: "CATEGORY_ID");
                    table.ForeignKey(
                        name: "FK_ENROLMENT_EVENT",
                        column: x => x.EVENT_ID,
                        principalSchema: "dbo",
                        principalTable: "EVENT",
                        principalColumn: "EVENT_ID");
                    table.ForeignKey(
                        name: "FK_ENROLMENT_PARTICIPANT",
                        column: x => x.PARTICIPANT_ID,
                        principalSchema: "dbo",
                        principalTable: "USER",
                        principalColumn: "USER_ID");
                });

            migrationBuilder.CreateTable(
                name: "RESULT",
                schema: "dbo",
                columns: table => new
                {
                    RESULT_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ENROLMENT_ID = table.Column<int>(type: "int", nullable: false),
                    FINISH_TIME = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    FINISHING_POSITION = table.Column<int>(type: "int", nullable: false),
                    IS_PUBLISHED = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_RESULT_IS_PUBLISHED"),
                    PUBLISHED_AT = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESULT", x => x.RESULT_ID);
                    table.UniqueConstraint("UQ_RESULT_ENROLMENT", x => x.ENROLMENT_ID);
                    table.CheckConstraint("CK_RESULT_POSITION", "[FINISHING_POSITION] > 0");
                    table.CheckConstraint("CK_RESULT_PUBLISHED", "([IS_PUBLISHED] = 0 AND [PUBLISHED_AT] IS NULL) OR ([IS_PUBLISHED] = 1 AND [PUBLISHED_AT] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RESULT_ENROLMENT",
                        column: x => x.ENROLMENT_ID,
                        principalSchema: "dbo",
                        principalTable: "ENROLMENT",
                        principalColumn: "ENROLMENT_ID");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ROLE",
                columns: new[] { "ROLE_ID", "ROLE_NAME" },
                values: new object[,]
                {
                    { 1, "Organiser" },
                    { 2, "Participant" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_CATEGORY_EVENT_NAME",
                schema: "dbo",
                table: "CATEGORY",
                columns: new[] { "EVENT_ID", "CATEGORY_NAME" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ENROLMENT_CATEGORY_ID",
                schema: "dbo",
                table: "ENROLMENT",
                column: "CATEGORY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_ENROLMENT_EVENT_ID",
                schema: "dbo",
                table: "ENROLMENT",
                column: "EVENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_EVENT_ORGANISER_ID",
                schema: "dbo",
                table: "EVENT",
                column: "ORGANISER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ROLE_ID",
                schema: "dbo",
                table: "USER",
                column: "ROLE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RESULT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ENROLMENT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "CATEGORY",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EVENT",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "USER",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ROLE",
                schema: "dbo");
        }
    }
}
