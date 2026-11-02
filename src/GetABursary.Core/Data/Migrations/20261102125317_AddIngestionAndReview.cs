using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // Generated migration code uses constant array arguments.

namespace GetABursary.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingestion_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_run", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingestion_run_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    picked_up_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_run_request", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingestion_run_request_source_source_id",
                        column: x => x.source_id,
                        principalTable: "source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "listing_report",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listing_report", x => x.id);
                    table.ForeignKey(
                        name: "fk_listing_report_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "raw_listing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    html = table.Column<string>(type: "text", nullable: false),
                    parsed = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    extracted = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    missed_runs = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_listing", x => x.id);
                    table.ForeignKey(
                        name: "fk_raw_listing_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_raw_listing_source_source_id",
                        column: x => x.source_id,
                        principalTable: "source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingestion_run_source",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discovered = table.Column<int>(type: "integer", nullable: false),
                    fetched = table.Column<int>(type: "integer", nullable: false),
                    unchanged = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<int>(type: "integer", nullable: false),
                    updated = table.Column<int>(type: "integer", nullable: false),
                    failed = table.Column<int>(type: "integer", nullable: false),
                    error_summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingestion_run_source", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingestion_run_source_ingestion_run_run_id",
                        column: x => x.run_id,
                        principalTable: "ingestion_run",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ingestion_run_source_source_source_id",
                        column: x => x.source_id,
                        principalTable: "source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    bursary_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_listing_id = table.Column<Guid>(type: "uuid", nullable: true),
                    listing_report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_item_bursary_bursary_id",
                        column: x => x.bursary_id,
                        principalTable: "bursary",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_review_item_listing_report_listing_report_id",
                        column: x => x.listing_report_id,
                        principalTable: "listing_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_review_item_raw_listing_raw_listing_id",
                        column: x => x.raw_listing_id,
                        principalTable: "raw_listing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_run_request_source_id",
                table: "ingestion_run_request",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_run_source_run_id",
                table: "ingestion_run_source",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingestion_run_source_source_id",
                table: "ingestion_run_source",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_listing_report_bursary_id",
                table: "listing_report",
                column: "bursary_id");

            migrationBuilder.CreateIndex(
                name: "ix_raw_listing_bursary_id",
                table: "raw_listing",
                column: "bursary_id");

            migrationBuilder.CreateIndex(
                name: "ix_raw_listing_source_id_url",
                table: "raw_listing",
                columns: new[] { "source_id", "url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_item_bursary_id",
                table: "review_item",
                column: "bursary_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_item_listing_report_id",
                table: "review_item",
                column: "listing_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_item_raw_listing_id",
                table: "review_item",
                column: "raw_listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_item_status_type",
                table: "review_item",
                columns: new[] { "status", "type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingestion_run_request");

            migrationBuilder.DropTable(
                name: "ingestion_run_source");

            migrationBuilder.DropTable(
                name: "review_item");

            migrationBuilder.DropTable(
                name: "ingestion_run");

            migrationBuilder.DropTable(
                name: "listing_report");

            migrationBuilder.DropTable(
                name: "raw_listing");
        }
    }
}
