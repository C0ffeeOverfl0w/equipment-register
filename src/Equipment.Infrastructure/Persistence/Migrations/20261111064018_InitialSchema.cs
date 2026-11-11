using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Equipment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_types", x => x.id);
                    table.CheckConstraint("ck_equipment_types_name", "name !~ '^[[:space:]]*$'");
                });

            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    equipment_type_id = table.Column<int>(type: "integer", nullable: false),
                    room = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment", x => x.id);
                    table.CheckConstraint("ck_equipment_inventory_number", "inventory_number ~ '^[A-Za-z0-9]{1,32}$'");
                    table.CheckConstraint("ck_equipment_name", "name !~ '^[[:space:]]*$'");
                    table.CheckConstraint("ck_equipment_room", "room BETWEEN 1 AND 1000");
                    table.CheckConstraint("ck_equipment_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_equipment_type",
                        column: x => x.equipment_type_id,
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_type_id",
                table: "equipment",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_equipment_inventory_number",
                table: "equipment",
                column: "inventory_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "equipment_types");
        }
    }
}
