using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopFresherz.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCardLast4ToCardNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename column (preserves existing rows).
            migrationBuilder.RenameColumn(
                name: "Last4",
                table: "SavedCards",
                newName: "CardNumber");

            // Widen the column from nvarchar(4) to nvarchar(19).
            migrationBuilder.AlterColumn<string>(
                name: "CardNumber",
                table: "SavedCards",
                type: "nvarchar(19)",
                maxLength: 19,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldMaxLength: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CardNumber",
                table: "SavedCards",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(19)",
                oldMaxLength: 19);

            migrationBuilder.RenameColumn(
                name: "CardNumber",
                table: "SavedCards",
                newName: "Last4");
        }
    }
}
