using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NextItemId",
                table: "SiteLibraryItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PreviousItemId",
                table: "SiteLibraryItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedItemIds",
                table: "SiteLibraryItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedItemIds",
                table: "SiteLibraryItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VideoContentId",
                table: "SiteLibraryItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ListeningScore = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    ReadingScore = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    WritingScore = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    VocabularyScore = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    SuggestionLevel = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteLibraryItems_VideoContentId",
                table: "SiteLibraryItems",
                column: "VideoContentId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateUsers_PhoneNumber",
                table: "CandidateUsers",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteLibraryItems_Contents_VideoContentId",
                table: "SiteLibraryItems",
                column: "VideoContentId",
                principalTable: "Contents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteLibraryItems_Contents_VideoContentId",
                table: "SiteLibraryItems");

            migrationBuilder.DropTable(
                name: "CandidateUsers");

            migrationBuilder.DropIndex(
                name: "IX_SiteLibraryItems_VideoContentId",
                table: "SiteLibraryItems");

            migrationBuilder.DropColumn(
                name: "NextItemId",
                table: "SiteLibraryItems");

            migrationBuilder.DropColumn(
                name: "PreviousItemId",
                table: "SiteLibraryItems");

            migrationBuilder.DropColumn(
                name: "RelatedItemIds",
                table: "SiteLibraryItems");

            migrationBuilder.DropColumn(
                name: "SuggestedItemIds",
                table: "SiteLibraryItems");

            migrationBuilder.DropColumn(
                name: "VideoContentId",
                table: "SiteLibraryItems");
        }
    }
}
