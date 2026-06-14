using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Richie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeAndExpenseDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incomes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_UserId_ExpenseId",
                table: "ExpenseDocuments",
                columns: new[] { "UserId", "ExpenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_UserId_Date",
                table: "Incomes",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseDocuments");

            migrationBuilder.DropTable(
                name: "Incomes");
        }
    }
}
