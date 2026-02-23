using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoBuddy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVehicleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_FK_ID",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "FK_ID",
                table: "Vehicles",
                newName: "FK_user_ID");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_FK_ID",
                table: "Vehicles",
                newName: "IX_Vehicles_FK_user_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_FK_user_ID",
                table: "Vehicles",
                column: "FK_user_ID",
                principalTable: "Users",
                principalColumn: "PK_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_FK_user_ID",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "FK_user_ID",
                table: "Vehicles",
                newName: "FK_ID");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_FK_user_ID",
                table: "Vehicles",
                newName: "IX_Vehicles_FK_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_FK_ID",
                table: "Vehicles",
                column: "FK_ID",
                principalTable: "Users",
                principalColumn: "PK_ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
