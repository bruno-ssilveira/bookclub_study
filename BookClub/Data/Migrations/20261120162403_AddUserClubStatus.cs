using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookClub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserClubStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_club_club_id",
                table: "user_club");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "user_club",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "ix_user_club_club_id_status",
                table: "user_club",
                columns: new[] { "club_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_club_club_id_status",
                table: "user_club");

            migrationBuilder.DropColumn(
                name: "status",
                table: "user_club");

            migrationBuilder.CreateIndex(
                name: "IX_user_club_club_id",
                table: "user_club",
                column: "club_id");
        }
    }
}
