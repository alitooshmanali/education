using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacementGameItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Subject",
                table: "SiteCourses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlacementGameItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnswerIndex = table.Column<int>(type: "int", nullable: true),
                    Word = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Line1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Line2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Hint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AudioContentId = table.Column<long>(type: "bigint", nullable: true),
                    SpeakText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StoryPrompt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacementGameItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlacementGameItems_Contents_AudioContentId",
                        column: x => x.AudioContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlacementGameItems_AudioContentId",
                table: "PlacementGameItems",
                column: "AudioContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlacementGameItems");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "SiteCourses");
        }
    }
}
