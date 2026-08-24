using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Portfolio.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_seeds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    seeded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_seeds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "education",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_education", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: false),
                    start_month = table.Column<int>(type: "integer", nullable: false),
                    end_year = table.Column<int>(type: "integer", nullable: false),
                    end_month = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experiences", x => x.id);
                    table.CheckConstraint("ck_experiences_end_month", "end_month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_experiences_period", "(end_year * 12 + end_month) >= (start_year * 12 + start_month)");
                    table.CheckConstraint("ck_experiences_start_month", "start_month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    availability = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_categories",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "social_links",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    display = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_social_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "education_translations",
                columns: table => new
                {
                    education_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    degree = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_education_translations", x => new { x.education_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_education_translations_education_education_id",
                        column: x => x.education_id,
                        principalTable: "education",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_highlights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_highlights", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_highlights_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_parallel_roles",
                columns: table => new
                {
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    parallel_experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_parallel_roles", x => new { x.experience_id, x.parallel_experience_id });
                    table.ForeignKey(
                        name: "fk_experience_parallel_roles_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_teams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    team = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_teams", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_teams_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_technologies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    technology = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_technologies", x => x.id);
                    table.ForeignKey(
                        name: "fk_experience_technologies_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_translations",
                columns: table => new
                {
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experience_translations", x => new { x.experience_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_experience_translations_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    experience_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    featured = table.Column<bool>(type: "boolean", nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_experiences_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profile_translations",
                columns: table => new
                {
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    headline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary_template = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_translations", x => new { x.profile_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_profile_translations_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "spoken_languages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    level = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spoken_languages", x => x.id);
                    table.ForeignKey(
                        name: "fk_spoken_languages_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_category_translations",
                columns: table => new
                {
                    category_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_category_translations", x => new { x.category_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_skill_category_translations_skill_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "skill_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    item = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_items_skill_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "skill_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    checked_on = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_sources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_technologies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    technology = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_technologies", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_technologies_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_translations",
                columns: table => new
                {
                    project_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    cv_summary = table.Column<string>(type: "text", nullable: true),
                    contribution = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_translations", x => new { x.project_id, x.language_code });
                    table.ForeignKey(
                        name: "fk_project_translations_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_experience_highlights_experience_id_language_code_ordinal",
                table: "experience_highlights",
                columns: new[] { "experience_id", "language_code", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experience_teams_experience_id_language_code_ordinal",
                table: "experience_teams",
                columns: new[] { "experience_id", "language_code", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experience_technologies_experience_id_ordinal",
                table: "experience_technologies",
                columns: new[] { "experience_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experiences_ordinal",
                table: "experiences",
                column: "ordinal");

            migrationBuilder.CreateIndex(
                name: "ix_project_sources_project_id_ordinal",
                table: "project_sources",
                columns: new[] { "project_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_technologies_project_id_ordinal",
                table: "project_technologies",
                columns: new[] { "project_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_experience_id",
                table: "projects",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_ordinal",
                table: "projects",
                column: "ordinal");

            migrationBuilder.CreateIndex(
                name: "ix_skill_categories_ordinal",
                table: "skill_categories",
                column: "ordinal");

            migrationBuilder.CreateIndex(
                name: "ix_skill_items_category_id_ordinal",
                table: "skill_items",
                columns: new[] { "category_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_social_links_ordinal",
                table: "social_links",
                column: "ordinal");

            migrationBuilder.CreateIndex(
                name: "ix_spoken_languages_profile_id_language_code_ordinal",
                table: "spoken_languages",
                columns: new[] { "profile_id", "language_code", "ordinal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_seeds");

            migrationBuilder.DropTable(
                name: "education_translations");

            migrationBuilder.DropTable(
                name: "experience_highlights");

            migrationBuilder.DropTable(
                name: "experience_parallel_roles");

            migrationBuilder.DropTable(
                name: "experience_teams");

            migrationBuilder.DropTable(
                name: "experience_technologies");

            migrationBuilder.DropTable(
                name: "experience_translations");

            migrationBuilder.DropTable(
                name: "profile_translations");

            migrationBuilder.DropTable(
                name: "project_sources");

            migrationBuilder.DropTable(
                name: "project_technologies");

            migrationBuilder.DropTable(
                name: "project_translations");

            migrationBuilder.DropTable(
                name: "skill_category_translations");

            migrationBuilder.DropTable(
                name: "skill_items");

            migrationBuilder.DropTable(
                name: "social_links");

            migrationBuilder.DropTable(
                name: "spoken_languages");

            migrationBuilder.DropTable(
                name: "education");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "skill_categories");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "experiences");
        }
    }
}
