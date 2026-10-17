using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteLibraryAndPageSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestions_Contents_ImageContentId",
                table: "QuizQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Contents_IntroImageContentId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Contents_ImageContentId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Contents_VoiceContentId",
                table: "Stories");

            migrationBuilder.CreateTable(
                name: "SiteLibraryItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BodyText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageContentId = table.Column<long>(type: "bigint", nullable: true),
                    AudioContentId = table.Column<long>(type: "bigint", nullable: true),
                    IsVoiceOnly = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteLibraryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteLibraryItems_Contents_AudioContentId",
                        column: x => x.AudioContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteLibraryItems_Contents_ImageContentId",
                        column: x => x.ImageContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SitePageSlots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SlotType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LinkHref = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ImageContentId = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitePageSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SitePageSlots_Contents_ImageContentId",
                        column: x => x.ImageContentId,
                        principalTable: "Contents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteLibraryItems_AudioContentId",
                table: "SiteLibraryItems",
                column: "AudioContentId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteLibraryItems_ImageContentId",
                table: "SiteLibraryItems",
                column: "ImageContentId");

            migrationBuilder.CreateIndex(
                name: "IX_SitePageSlots_ImageContentId",
                table: "SitePageSlots",
                column: "ImageContentId");

            migrationBuilder.CreateIndex(
                name: "IX_SitePageSlots_PageKey_SlotType_SortOrder",
                table: "SitePageSlots",
                columns: new[] { "PageKey", "SlotType", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestions_Contents_ImageContentId",
                table: "QuizQuestions",
                column: "ImageContentId",
                principalTable: "Contents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Contents_IntroImageContentId",
                table: "Quizzes",
                column: "IntroImageContentId",
                principalTable: "Contents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Contents_ImageContentId",
                table: "Stories",
                column: "ImageContentId",
                principalTable: "Contents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Contents_VoiceContentId",
                table: "Stories",
                column: "VoiceContentId",
                principalTable: "Contents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizQuestions_Contents_ImageContentId",
                table: "QuizQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Contents_IntroImageContentId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Contents_ImageContentId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Contents_VoiceContentId",
                table: "Stories");

            migrationBuilder.DropTable(
                name: "SiteLibraryItems");

            migrationBuilder.DropTable(
                name: "SitePageSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestions_Contents_ImageContentId",
                table: "QuizQuestions",
                column: "ImageContentId",
                principalTable: "Contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Contents_IntroImageContentId",
                table: "Quizzes",
                column: "IntroImageContentId",
                principalTable: "Contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Contents_ImageContentId",
                table: "Stories",
                column: "ImageContentId",
                principalTable: "Contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Contents_VoiceContentId",
                table: "Stories",
                column: "VoiceContentId",
                principalTable: "Contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
