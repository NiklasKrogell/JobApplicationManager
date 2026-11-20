using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationManager.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitiesAndStatusText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "JobApplications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            // Vanhat numeroarvot (InitialCreate-ajan enum) → enumin nimet.
            // CAST toimii riippumatta siitä, onko sarake tässä kohtaa jo TEXT vai vielä INTEGER.
            migrationBuilder.Sql("""
                UPDATE JobApplications SET Status = CASE CAST(Status AS TEXT)
                    WHEN '0' THEN 'Applied'
                    WHEN '1' THEN 'Interviewing'
                    WHEN '2' THEN 'Offered'
                    WHEN '3' THEN 'Rejected'
                    ELSE Status
                END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "JobPostingUrl",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    NewStatus = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_JobApplicationId",
                table: "Activities",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropColumn(
                name: "JobPostingUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobApplications");

            // Nimet takaisin vanhan enumin numeroiksi. Accepted/Withdrawn puuttuivat
            // vanhasta enumista, joten ne kuvataan lähimpiin: Offered (2) / Rejected (3).
            migrationBuilder.Sql("""
                UPDATE JobApplications SET Status = CASE Status
                    WHEN 'Applied' THEN 0
                    WHEN 'Interviewing' THEN 1
                    WHEN 'Offered' THEN 2
                    WHEN 'Accepted' THEN 2
                    WHEN 'Rejected' THEN 3
                    WHEN 'Withdrawn' THEN 3
                    ELSE 0
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "JobApplications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
