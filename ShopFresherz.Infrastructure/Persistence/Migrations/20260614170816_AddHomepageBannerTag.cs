using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepageBannerTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "HomepageBanners",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                table: "HomepageBanners");
        }
    }
}
