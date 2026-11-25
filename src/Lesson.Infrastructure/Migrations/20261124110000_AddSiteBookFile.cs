using Lesson.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lesson.Infrastructure.Migrations;

[DbContext(typeof(LessonDbContext))]
[Migration("20261124110000_AddSiteBookFile")]
public partial class AddSiteBookFile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(name: "FileContentId", table: "SiteBooks", type: "bigint", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_SiteBooks_FileContentId", table: "SiteBooks", column: "FileContentId");
        migrationBuilder.AddForeignKey(name: "FK_SiteBooks_Contents_FileContentId", table: "SiteBooks", column: "FileContentId", principalTable: "Contents", principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_SiteBooks_Contents_FileContentId", table: "SiteBooks");
        migrationBuilder.DropIndex(name: "IX_SiteBooks_FileContentId", table: "SiteBooks");
        migrationBuilder.DropColumn(name: "FileContentId", table: "SiteBooks");
    }
}
