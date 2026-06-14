using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Richie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseRecurring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecurringId",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseRecurrings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    SpentBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    SpentFor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRunDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseRecurrings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseRecurrings_IsEnabled_NextRunDateUtc",
                table: "ExpenseRecurrings",
                columns: new[] { "IsEnabled", "NextRunDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseRecurrings");

            migrationBuilder.DropColumn(
                name: "RecurringId",
                table: "Expenses");
        }
    }
}
