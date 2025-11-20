using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ArchitecturalDreamMachineBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddArchitecturalParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Designs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LotSize = table.Column<double>(type: "REAL", nullable: false),
                    StyleKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StyleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RoofType = table.Column<string>(type: "TEXT", nullable: false),
                    WindowStyle = table.Column<string>(type: "TEXT", nullable: false),
                    RoomCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    Texture = table.Column<string>(type: "TEXT", nullable: false),
                    TypicalCeilingHeight = table.Column<double>(type: "REAL", nullable: false),
                    TypicalStories = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildingShape = table.Column<string>(type: "TEXT", nullable: false),
                    WindowToWallRatio = table.Column<double>(type: "REAL", nullable: false),
                    FoundationType = table.Column<string>(type: "TEXT", nullable: false),
                    ExteriorMaterial = table.Column<string>(type: "TEXT", nullable: false),
                    RoofPitch = table.Column<double>(type: "REAL", nullable: false),
                    HasParapet = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasEaves = table.Column<bool>(type: "INTEGER", nullable: false),
                    EavesOverhang = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StyleTemplates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StyleTemplates",
                columns: new[] { "Id", "BuildingShape", "Color", "EavesOverhang", "ExteriorMaterial", "FoundationType", "HasEaves", "HasParapet", "Name", "RoofPitch", "RoofType", "RoomCount", "Texture", "TypicalCeilingHeight", "TypicalStories", "WindowStyle", "WindowToWallRatio" },
                values: new object[,]
                {
                    { 1, "rectangular", "gray", 0.0, "concrete", "slab", false, true, "Brutalist", 0.0, "flat", 4, "concrete", 12.0, 1, "small", 0.10000000000000001 },
                    { 2, "l-shape", "cream", 2.0, "wood siding", "crawlspace", true, false, "Victorian", 8.0, "gabled", 6, "wood", 9.0, 2, "ornate", 0.20000000000000001 },
                    { 3, "rectangular", "white", 0.0, "stucco", "slab", false, true, "Modern", 0.0, "flat", 5, "glass", 10.0, 2, "large", 0.29999999999999999 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Designs");

            migrationBuilder.DropTable(
                name: "StyleTemplates");
        }
    }
}
