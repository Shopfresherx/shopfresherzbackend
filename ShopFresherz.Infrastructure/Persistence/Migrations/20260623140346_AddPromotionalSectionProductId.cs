using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionalSectionProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "PromotionalSections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalSections_SectionKey_ProductId",
                table: "PromotionalSections",
                columns: new[] { "SectionKey", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromotionalSections_SectionKey_ProductId",
                table: "PromotionalSections");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "PromotionalSections");
        }
    }
}
