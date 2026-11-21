using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationManager.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostingKeywordsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobPostingText",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeywordsJson",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropColumn(
                name: "JobPostingText",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "KeywordsJson",
                table: "JobApplications");
        }
    }
}
