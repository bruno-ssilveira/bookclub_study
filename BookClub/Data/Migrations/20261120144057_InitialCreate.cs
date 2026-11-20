using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookClub.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    surname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_user_created_by",
                        column: x => x.created_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "book",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: true),
                    pages = table.Column<int>(type: "integer", nullable: true),
                    cover_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    published_year = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_book", x => x.id);
                    table.ForeignKey(
                        name: "fk_book_user_created_by",
                        column: x => x.created_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_book_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "club",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    invite_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club", x => x.id);
                    table.ForeignKey(
                        name: "fk_club_user_created_by",
                        column: x => x.created_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_club_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "club_book",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    club_id = table.Column<int>(type: "integer", nullable: false),
                    book_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    target_finish_date = table.Column<DateOnly>(type: "date", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_club_book", x => x.id);
                    table.ForeignKey(
                        name: "fk_club_book_book_book_id",
                        column: x => x.book_id,
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_club_book_club_club_id",
                        column: x => x.club_id,
                        principalTable: "club",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_club_book_user_created_by",
                        column: x => x.created_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_club_book_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_club",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    club_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_club", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_club_club_club_id",
                        column: x => x.club_id,
                        principalTable: "club",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_club_user_created_by",
                        column: x => x.created_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_club_user_updated_by",
                        column: x => x.updated_by,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_club_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_created_by",
                table: "book",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_book_external_id",
                table: "book",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_isbn",
                table: "book",
                column: "isbn");

            migrationBuilder.CreateIndex(
                name: "IX_book_updated_by",
                table: "book",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_club_created_by",
                table: "club",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_club_invite_code",
                table: "club",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_club_updated_by",
                table: "club",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_club_book_book_id",
                table: "club_book",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_club_book_club_id_status",
                table: "club_book",
                columns: new[] { "club_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_club_book_created_by",
                table: "club_book",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_club_book_updated_by",
                table: "club_book",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_created_by",
                table: "user",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_updated_by",
                table: "user",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_club_club_id",
                table: "user_club",
                column: "club_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_club_created_by",
                table: "user_club",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_club_updated_by",
                table: "user_club",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_user_club_user_id_club_id",
                table: "user_club",
                columns: new[] { "user_id", "club_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "club_book");

            migrationBuilder.DropTable(
                name: "user_club");

            migrationBuilder.DropTable(
                name: "book");

            migrationBuilder.DropTable(
                name: "club");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
