using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTaskBasedAiRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiTasks",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    task_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    route_hint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    intent_embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    task_specific_system_prompt = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AiTaskTools",
                columns: table => new
                {
                    task_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tool_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_task_tools", x => new { x.task_id, x.tool_id });
                    table.ForeignKey(
                        name: "fk_ai_task_tools_ai_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "AiTasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ai_task_tools_ai_tools_tool_id",
                        column: x => x.tool_id,
                        principalTable: "AiTools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_intent_embedding",
                table: "AiTasks",
                column: "intent_embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_tasks_route_hint",
                table: "AiTasks",
                column: "route_hint");

            migrationBuilder.CreateIndex(
                name: "ix_ai_task_tools_tool_id",
                table: "AiTaskTools",
                column: "tool_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTaskTools");

            migrationBuilder.DropTable(
                name: "AiTasks");
        }
    }
}
