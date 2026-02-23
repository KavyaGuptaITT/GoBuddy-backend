using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoBuddy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleNo",
                table: "Vehicles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleNo",
                table: "Vehicles",
                column: "VehicleNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles",
                column: "DriverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VehicleNo",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleNo",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Users_DriverId",
                table: "Vehicles",
                column: "DriverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
