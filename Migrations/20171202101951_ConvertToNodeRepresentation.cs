using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Skills.Migrations
{
    public partial class ConvertToNodeRepresentation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TagModel",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NodeModelid = table.Column<long>(type: "INTEGER", nullable: true),
                    tag = table.Column<string>(type: "TEXT", nullable: true),
                    value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagModel", x => x.id);
                    table.ForeignKey(
                        name: "FK_TagModel_Nodes_NodeModelid",
                        column: x => x.NodeModelid,
                        principalTable: "Nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagModel_NodeModelid",
                table: "TagModel",
                column: "NodeModelid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagModel");

            migrationBuilder.DropTable(
                name: "Nodes");
        }
    }
}
