using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration code uses constant array arguments.

namespace GetABursary.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBursaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "funder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funder", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    crawl_delay_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 3000),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bursary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    funder_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    value_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    coverage = table.Column<string[]>(type: "text[]", nullable: false),
                    work_back_required = table.Column<bool>(type: "boolean", nullable: true),
                    work_back_details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    study_levels = table.Column<string[]>(type: "text[]", nullable: false),
                    min_average = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    citizenship = table.Column<string>(type: "text", nullable: false),
                    provinces = table.Column<string[]>(type: "text[]", nullable: false),
                    all_fields_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    open_date = table.Column<DateOnly>(type: "date", nullable: true),
                    closing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    apply_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    extraction_confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    locked_fields = table.Column<List<string>>(type: "text[]", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    min_subject_requirements = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bursary", x => x.id);
                    table.ForeignKey(
                        name: "fk_bursary_funders_funder_id",
                        column: x => x.funder_id,
                        principalTable: "funder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "bursary_field",
                columns: table => new
                {
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_of_study_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bursary_field", x => new { x.bursary_id, x.field_of_study_id });
                    table.ForeignKey(
                        name: "fk_bursary_field_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bursary_field_fields_of_study_field_of_study_id",
                        column: x => x.field_of_study_id,
                        principalTable: "field_of_study",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bursary_institution",
                columns: table => new
                {
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bursary_institution", x => new { x.bursary_id, x.institution_id });
                    table.ForeignKey(
                        name: "fk_bursary_institution_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bursary_institution_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "institution",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bursary_qualification",
                columns: table => new
                {
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bursary_qualification", x => new { x.bursary_id, x.qualification_id });
                    table.ForeignKey(
                        name: "fk_bursary_qualification_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bursary_qualification_qualifications_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bursary_source",
                columns: table => new
                {
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bursary_source", x => new { x.bursary_id, x.source_id, x.source_url });
                    table.ForeignKey(
                        name: "fk_bursary_source_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bursary_source_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bursary_funder_id",
                table: "bursary",
                column: "funder_id");

            migrationBuilder.CreateIndex(
                name: "ix_bursary_normalized_name",
                table: "bursary",
                column: "normalized_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_bursary_slug",
                table: "bursary",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bursary_status_closing_date",
                table: "bursary",
                columns: new[] { "status", "closing_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bursary_field_field_of_study_id",
                table: "bursary_field",
                column: "field_of_study_id");

            migrationBuilder.CreateIndex(
                name: "ix_bursary_institution_institution_id",
                table: "bursary_institution",
                column: "institution_id");

            migrationBuilder.CreateIndex(
                name: "ix_bursary_qualification_qualification_id",
                table: "bursary_qualification",
                column: "qualification_id");

            migrationBuilder.CreateIndex(
                name: "ix_bursary_source_source_id",
                table: "bursary_source",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_funder_normalized_name",
                table: "funder",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_source_key",
                table: "source",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bursary_field");

            migrationBuilder.DropTable(
                name: "bursary_institution");

            migrationBuilder.DropTable(
                name: "bursary_qualification");

            migrationBuilder.DropTable(
                name: "bursary_source");

            migrationBuilder.DropTable(
                name: "bursary");

            migrationBuilder.DropTable(
                name: "source");

            migrationBuilder.DropTable(
                name: "funder");
        }
    }
}
