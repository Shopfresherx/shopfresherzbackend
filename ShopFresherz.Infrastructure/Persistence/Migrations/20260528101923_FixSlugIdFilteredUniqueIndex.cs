using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSlugIdFilteredUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections",
                column: "SlugId",
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalSections_SlugId",
                table: "PromotionalSections",
                column: "SlugId",
                unique: true);
        }
    }
}
