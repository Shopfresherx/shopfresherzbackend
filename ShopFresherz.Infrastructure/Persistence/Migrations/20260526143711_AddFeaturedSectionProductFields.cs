using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedSectionProductFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Badge",
                table: "FeaturedSections",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtaText",
                table: "FeaturedSections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "FeaturedSections",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Price",
                table: "FeaturedSections",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "FeaturedSections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "FeaturedSections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badge",
                table: "FeaturedSections");

            migrationBuilder.DropColumn(
                name: "CtaText",
                table: "FeaturedSections");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "FeaturedSections");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "FeaturedSections");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "FeaturedSections");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "FeaturedSections");
        }
    }
}
