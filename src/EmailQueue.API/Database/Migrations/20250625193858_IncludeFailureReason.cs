using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailQueue.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class IncludeFailureReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "EmailTasks",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "EmailTasks");
        }
    }
}
