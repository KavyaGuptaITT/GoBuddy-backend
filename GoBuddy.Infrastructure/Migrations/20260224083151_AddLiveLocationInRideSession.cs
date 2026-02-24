using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoBuddy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveLocationInRideSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RideSessions_CurrentLatitude_CurrentLongitude",
                table: "RideSessions",
                columns: new[] { "CurrentLatitude", "CurrentLongitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RideSessions_CurrentLatitude_CurrentLongitude",
                table: "RideSessions");
        }
    }
}
