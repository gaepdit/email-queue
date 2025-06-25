using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailQueue.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Counter = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    From = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FromName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Recipients = table.Column<string>(type: "TEXT", nullable: false),
                    CopyRecipients = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                    IsHtml = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTasks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTasks");
        }
    }
}
