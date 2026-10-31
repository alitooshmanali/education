using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteCourses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    TuitionAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    SessionCount = table.Column<int>(type: "int", nullable: false),
                    SessionDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ImageContentId = table.Column<long>(type: "bigint", nullable: true),
                    VideoContentId = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteCourses_Contents_ImageContentId",
                        column: x => x.ImageContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteCourses_Contents_VideoContentId",
                        column: x => x.VideoContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteCourses_ImageContentId",
                table: "SiteCourses",
                column: "ImageContentId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteCourses_VideoContentId",
                table: "SiteCourses",
                column: "VideoContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteCourses");
        }
    }
}
