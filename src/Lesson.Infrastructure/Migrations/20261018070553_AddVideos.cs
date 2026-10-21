using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideos : Migration
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

            migrationBuilder.AddColumn<long>(
                name: "VideoContentId",
                table: "SiteLibraryItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteLibraryItems_VideoContentId",
                table: "SiteLibraryItems",
                column: "VideoContentId");

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
                name: "VideoContentId",
                table: "SiteLibraryItems");
        }
    }
}
