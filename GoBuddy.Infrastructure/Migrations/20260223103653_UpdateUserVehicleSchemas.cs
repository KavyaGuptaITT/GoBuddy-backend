using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoBuddy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserVehicleSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "DriverId",
                table: "Vehicles",
                newName: "FK_ID");

            migrationBuilder.RenameColumn(
                name: "VehicleId",
                table: "Vehicles",
                newName: "PK_ID");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_DriverId",
                table: "Vehicles",
                newName: "IX_Vehicles_FK_ID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "PK_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_FK_ID",
                table: "Vehicles",
                column: "FK_ID",
                principalTable: "Users",
                principalColumn: "PK_ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_FK_ID",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "FK_ID",
                table: "Vehicles",
                newName: "DriverId");

            migrationBuilder.RenameColumn(
                name: "PK_ID",
                table: "Vehicles",
                newName: "VehicleId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_FK_ID",
                table: "Vehicles",
                newName: "IX_Vehicles_DriverId");

            migrationBuilder.RenameColumn(
                name: "PK_ID",
                table: "Users",
                newName: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles",
                column: "DriverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
