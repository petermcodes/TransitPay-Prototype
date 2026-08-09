using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_audit_logs",
                columns: table => new
                {
                    audit_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    actor_hash = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_audit_logs", x => x.audit_id);
                });

            migrationBuilder.CreateTable(
                name: "discount_types",
                columns: table => new
                {
                    discount_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_types", x => x.discount_type_id);
                    table.CheckConstraint("CK_discount_types_discount_percentage_range", "\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "driver_delete_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_delete_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "driver_edit_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_edit_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "fare_matrix_delete_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fare_matrix_delete_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "fare_matrix_edit_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fare_matrix_edit_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "passenger_delete_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_delete_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "passenger_edit_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_edit_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "terminal_delete_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminal_delete_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "terminal_edit_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_record_id = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    performed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_data = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminal_edit_history", x => x.history_id);
                });

            migrationBuilder.CreateTable(
                name: "terminals",
                columns: table => new
                {
                    terminal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    terminal_name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminals", x => x.terminal_id);
                });

            migrationBuilder.CreateTable(
                name: "discount_programs",
                columns: table => new
                {
                    discount_program_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discount_type_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_programs", x => x.discount_program_id);
                    table.CheckConstraint("CK_discount_programs_discount_percentage_range", "\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100");
                    table.ForeignKey(
                        name: "FK_discount_programs_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "discount_type_id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    mobile_number = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    plate_number = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fare_rules",
                columns: table => new
                {
                    fare_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    origin_terminal_id = table.Column<int>(type: "integer", nullable: false),
                    destination_terminal_id = table.Column<int>(type: "integer", nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    passenger_type = table.Column<int>(type: "integer", nullable: false),
                    fare_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fare_rules", x => x.fare_id);
                    table.CheckConstraint("CK_fare_rules_fare_amount_non_negative", "\"fare_amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_fare_rules_terminals_destination_terminal_id",
                        column: x => x.destination_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fare_rules_terminals_origin_terminal_id",
                        column: x => x.origin_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    card_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    card_number = table.Column<string>(type: "text", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    passenger_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.card_id);
                    table.ForeignKey(
                        name: "FK_cards_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked = table.Column<bool>(type: "boolean", nullable: false),
                    replaced_by_token_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.token_id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    trip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    bus_id = table.Column<int>(type: "integer", nullable: true),
                    origin_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    final_destination_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    current_boarding_origin_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    boarding_origin_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    route_name = table.Column<string>(type: "text", nullable: false),
                    trip_status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    passenger_count = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.trip_id);
                    table.ForeignKey(
                        name: "FK_trips_terminals_current_boarding_origin_terminal_id",
                        column: x => x.current_boarding_origin_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_terminals_final_destination_terminal_id",
                        column: x => x.final_destination_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_terminals_origin_terminal_id",
                        column: x => x.origin_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_users_driver_id",
                        column: x => x.driver_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_applications",
                columns: table => new
                {
                    discount_application_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    discount_type_id = table.Column<int>(type: "integer", nullable: false),
                    discount_program_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_document = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_applications", x => x.discount_application_id);
                    table.ForeignKey(
                        name: "FK_discount_applications_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discount_applications_discount_programs_discount_program_id",
                        column: x => x.discount_program_id,
                        principalTable: "discount_programs",
                        principalColumn: "discount_program_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discount_applications_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "discount_type_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discount_applications_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discount_applications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "passenger_discounts",
                columns: table => new
                {
                    passenger_discount_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    discount_program_id = table.Column<int>(type: "integer", nullable: true),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_discounts", x => x.passenger_discount_id);
                    table.ForeignKey(
                        name: "FK_passenger_discounts_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_passenger_discounts_discount_programs_discount_program_id",
                        column: x => x.discount_program_id,
                        principalTable: "discount_programs",
                        principalColumn: "discount_program_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_passenger_discounts_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qr_codes",
                columns: table => new
                {
                    qr_code_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_codes", x => x.qr_code_id);
                    table.ForeignKey(
                        name: "FK_qr_codes_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_plans",
                columns: table => new
                {
                    plan_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    origin_terminal_id = table.Column<int>(type: "integer", nullable: false),
                    destination_terminal_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    normal_fare = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    final_fare_price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_plans", x => x.plan_id);
                    table.ForeignKey(
                        name: "FK_trip_plans_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trip_plans_terminals_destination_terminal_id",
                        column: x => x.destination_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trip_plans_terminals_origin_terminal_id",
                        column: x => x.origin_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trip_plans_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    wallet_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.wallet_id);
                    table.CheckConstraint("CK_wallets_balance_non_negative", "\"balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_wallets_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: true),
                    driver_id = table.Column<int>(type: "integer", nullable: true),
                    trip_id = table.Column<int>(type: "integer", nullable: true),
                    origin_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    origin_terminal_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    terminal_id = table.Column<int>(type: "integer", nullable: true),
                    destination_terminal_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fare_id = table.Column<int>(type: "integer", nullable: true),
                    regular_fare = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                    final_fare = table.Column<decimal>(type: "numeric", nullable: false),
                    remaining_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_type_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    transaction_type = table.Column<int>(type: "integer", nullable: false),
                    transaction_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    transaction_reference_number = table.Column<string>(type: "text", nullable: true),
                    reference_number = table.Column<string>(type: "text", nullable: true),
                    payment_request_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    payment_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.transaction_id);
                    table.CheckConstraint("CK_transactions_amount_non_negative", "\"amount\" >= 0");
                    table.CheckConstraint("CK_transactions_discount_amount_non_negative", "\"discount_amount\" IS NULL OR \"discount_amount\" >= 0");
                    table.CheckConstraint("CK_transactions_discount_percentage_range", "\"discount_percentage\" IS NULL OR (\"discount_percentage\" >= 0 AND \"discount_percentage\" <= 100)");
                    table.CheckConstraint("CK_transactions_final_fare_non_negative", "\"final_fare\" >= 0");
                    table.CheckConstraint("CK_transactions_regular_fare_non_negative", "\"regular_fare\" >= 0");
                    table.ForeignKey(
                        name: "FK_transactions_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "discount_type_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_fare_rules_fare_id",
                        column: x => x.fare_id,
                        principalTable: "fare_rules",
                        principalColumn: "fare_id");
                    table.ForeignKey(
                        name: "FK_transactions_terminals_origin_terminal_id",
                        column: x => x.origin_terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_terminals_terminal_id",
                        column: x => x.terminal_id,
                        principalTable: "terminals",
                        principalColumn: "terminal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "trip_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_users_driver_id",
                        column: x => x.driver_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_created_at",
                table: "auth_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_event_type",
                table: "auth_audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_auth_audit_logs_user_id",
                table: "auth_audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cards_card_number",
                table: "cards",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_user_id",
                table: "cards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_approved_by",
                table: "discount_applications",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_card_id",
                table: "discount_applications",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_discount_program_id",
                table: "discount_applications",
                column: "discount_program_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_discount_type_id",
                table: "discount_applications",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_status",
                table: "discount_applications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_discount_applications_user_id",
                table: "discount_applications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_programs_discount_type_id",
                table: "discount_programs",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_programs_name",
                table: "discount_programs",
                column: "name",
                unique: true,
                filter: "name IS NOT NULL AND name <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_discount_types_name",
                table: "discount_types",
                column: "name",
                unique: true,
                filter: "name IS NOT NULL AND name <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_driver_delete_history_operation",
                table: "driver_delete_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_driver_delete_history_original_record_id",
                table: "driver_delete_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_delete_history_performed_at",
                table: "driver_delete_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_driver_edit_history_operation",
                table: "driver_edit_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_driver_edit_history_original_record_id",
                table: "driver_edit_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_edit_history_performed_at",
                table: "driver_edit_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_delete_history_operation",
                table: "fare_matrix_delete_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_delete_history_original_record_id",
                table: "fare_matrix_delete_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_delete_history_performed_at",
                table: "fare_matrix_delete_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_edit_history_operation",
                table: "fare_matrix_edit_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_edit_history_original_record_id",
                table: "fare_matrix_edit_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_fare_matrix_edit_history_performed_at",
                table: "fare_matrix_edit_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_fare_rules_destination_terminal_id",
                table: "fare_rules",
                column: "destination_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_fare_rules_origin_terminal_id_destination_terminal_id",
                table: "fare_rules",
                columns: new[] { "origin_terminal_id", "destination_terminal_id" },
                unique: true,
                filter: "is_active = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_delete_history_operation",
                table: "passenger_delete_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_delete_history_original_record_id",
                table: "passenger_delete_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_delete_history_performed_at",
                table: "passenger_delete_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_discounts_approved_by",
                table: "passenger_discounts",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_discounts_card_id",
                table: "passenger_discounts",
                column: "card_id",
                unique: true,
                filter: "status = 0");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_discounts_discount_program_id",
                table: "passenger_discounts",
                column: "discount_program_id");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_discounts_status",
                table: "passenger_discounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_edit_history_operation",
                table: "passenger_edit_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_edit_history_original_record_id",
                table: "passenger_edit_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_passenger_edit_history_performed_at",
                table: "passenger_edit_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_card_id",
                table: "qr_codes",
                column: "card_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_card_id_is_active",
                table: "qr_codes",
                columns: new[] { "card_id", "is_active" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_token",
                table: "qr_codes",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id_token",
                table: "refresh_tokens",
                columns: new[] { "user_id", "token" });

            migrationBuilder.CreateIndex(
                name: "IX_terminal_delete_history_operation",
                table: "terminal_delete_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_delete_history_original_record_id",
                table: "terminal_delete_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_delete_history_performed_at",
                table: "terminal_delete_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_edit_history_operation",
                table: "terminal_edit_history",
                column: "operation");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_edit_history_original_record_id",
                table: "terminal_edit_history",
                column: "original_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_edit_history_performed_at",
                table: "terminal_edit_history",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "IX_terminals_terminal_name",
                table: "terminals",
                column: "terminal_name",
                unique: true,
                filter: "terminal_name IS NOT NULL AND terminal_name <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_card_id",
                table: "transactions",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_card_id_created_at",
                table: "transactions",
                columns: new[] { "card_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_discount_type_id",
                table: "transactions",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_driver_id",
                table: "transactions",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_fare_id",
                table: "transactions",
                column: "fare_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_origin_terminal_id",
                table: "transactions",
                column: "origin_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_payment_request_key",
                table: "transactions",
                column: "payment_request_key",
                unique: true,
                filter: "payment_request_key IS NOT NULL AND status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_reference_number",
                table: "transactions",
                column: "reference_number",
                unique: true,
                filter: "reference_number IS NOT NULL AND reference_number <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_status",
                table: "transactions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_terminal_id",
                table: "transactions",
                column: "terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_reference_number",
                table: "transactions",
                column: "transaction_reference_number",
                unique: true,
                filter: "transaction_reference_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_type",
                table: "transactions",
                column: "transaction_type");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_trip_id",
                table: "transactions",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_plans_card_id",
                table: "trip_plans",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_plans_destination_terminal_id",
                table: "trip_plans",
                column: "destination_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_plans_origin_terminal_id",
                table: "trip_plans",
                column: "origin_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_trip_plans_user_id",
                table: "trip_plans",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_current_boarding_origin_terminal_id",
                table: "trips",
                column: "current_boarding_origin_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_driver_id",
                table: "trips",
                column: "driver_id",
                unique: true,
                filter: "trip_status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_trips_driver_id_trip_status",
                table: "trips",
                columns: new[] { "driver_id", "trip_status" });

            migrationBuilder.CreateIndex(
                name: "IX_trips_ended_at",
                table: "trips",
                column: "ended_at");

            migrationBuilder.CreateIndex(
                name: "IX_trips_final_destination_terminal_id",
                table: "trips",
                column: "final_destination_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_origin_terminal_id",
                table: "trips",
                column: "origin_terminal_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_started_at",
                table: "trips",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_trips_trip_status",
                table: "trips",
                column: "trip_status");

            migrationBuilder.CreateIndex(
                name: "IX_users_mobile_number",
                table: "users",
                column: "mobile_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallets_card_id",
                table: "wallets",
                column: "card_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_audit_logs");

            migrationBuilder.DropTable(
                name: "discount_applications");

            migrationBuilder.DropTable(
                name: "driver_delete_history");

            migrationBuilder.DropTable(
                name: "driver_edit_history");

            migrationBuilder.DropTable(
                name: "fare_matrix_delete_history");

            migrationBuilder.DropTable(
                name: "fare_matrix_edit_history");

            migrationBuilder.DropTable(
                name: "passenger_delete_history");

            migrationBuilder.DropTable(
                name: "passenger_discounts");

            migrationBuilder.DropTable(
                name: "passenger_edit_history");

            migrationBuilder.DropTable(
                name: "qr_codes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "terminal_delete_history");

            migrationBuilder.DropTable(
                name: "terminal_edit_history");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "trip_plans");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "discount_programs");

            migrationBuilder.DropTable(
                name: "fare_rules");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "discount_types");

            migrationBuilder.DropTable(
                name: "terminals");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
