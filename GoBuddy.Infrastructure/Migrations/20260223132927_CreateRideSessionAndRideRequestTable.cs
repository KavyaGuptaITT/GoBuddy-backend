using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoBuddy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateRideSessionAndRideRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RideSessions",
                columns: table => new
                {
                    PK_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FK_Driver_ID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: false),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: false),
                    TotalPassengersInRide = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideSessions", x => x.PK_ID);
                    table.ForeignKey(
                        name: "FK_RideSessions_Users_FK_Driver_ID",
                        column: x => x.FK_Driver_ID,
                        principalTable: "Users",
                        principalColumn: "PK_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RideRequests",
                columns: table => new
                {
                    PK_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FK_RideSession_ID = table.Column<int>(type: "int", nullable: false),
                    FK_User_ID = table.Column<int>(type: "int", nullable: false),
                    PickupLatitude = table.Column<double>(type: "float", nullable: false),
                    PickupLongitude = table.Column<double>(type: "float", nullable: false),
                    DropupLatitude = table.Column<double>(type: "float", nullable: false),
                    DropupLongitude = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideRequests", x => x.PK_ID);
                    table.ForeignKey(
                        name: "FK_RideRequests_RideSessions_FK_RideSession_ID",
                        column: x => x.FK_RideSession_ID,
                        principalTable: "RideSessions",
                        principalColumn: "PK_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RideRequests_Users_FK_User_ID",
                        column: x => x.FK_User_ID,
                        principalTable: "Users",
                        principalColumn: "PK_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_FK_RideSession_ID",
                table: "RideRequests",
                column: "FK_RideSession_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_FK_User_ID",
                table: "RideRequests",
                column: "FK_User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RideSessions_FK_Driver_ID",
                table: "RideSessions",
                column: "FK_Driver_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideRequests");

            migrationBuilder.DropTable(
                name: "RideSessions");
        }
    }
}
