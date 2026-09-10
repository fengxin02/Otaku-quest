using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtakuQuest.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRepeatableTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionCount",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeatable",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCompletedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionCount",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsRepeatable",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastCompletedAt",
                table: "Tasks");
        }
    }
}
