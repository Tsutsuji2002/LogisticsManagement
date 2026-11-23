using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabaseSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "carrier");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "configuration");

            migrationBuilder.EnsureSchema(
                name: "shipping");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.EnsureSchema(
                name: "warehouse");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "BackgroundJobLogs",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExecutionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsSucceeded = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    LogDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carriers",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TrackingUrlTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Alpha3Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumericCode = table.Column<int>(type: "int", nullable: true),
                    PhonePrefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    ExchangeRateToDefault = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequences",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    Increment = table.Column<int>(type: "int", nullable: false),
                    NumberLength = table.Column<int>(type: "int", nullable: false),
                    CurrentFormat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResetFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastResetDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "catalog",
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QueryDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrierApiLogs",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestPayloadMasked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayloadMasked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierApiLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierApiLogs_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierConfigurations",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApiSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WebhookSecret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierConfigurations_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierServices",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedTransitHours = table.Column<int>(type: "int", nullable: true),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    MaxLengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    IsCodSupported = table.Column<bool>(type: "bit", nullable: false),
                    IsInsuranceSupported = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierServices_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierSyncJobs",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EndTime = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalRecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalSuccessCount = table.Column<int>(type: "int", nullable: false),
                    TotalFailureCount = table.Column<int>(type: "int", nullable: false),
                    ErrorLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierSyncJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierSyncJobs_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierTrackingMappings",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalStatusCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalStatusName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InternalShipmentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InternalSubStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierTrackingMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierTrackingMappings_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarrierWebhookLogs",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WebhookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedSuccessfully = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierWebhookLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierWebhookLogs_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Provinces_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "configuration",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRateCards",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaseCurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRateCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingRateCards_Currencies_BaseCurrencyId",
                        column: x => x.BaseCurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduleCron = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OutputFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientEmails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledReports_ReportDefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalSchema: "notification",
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    IsPerishable = table.Column<bool>(type: "bit", nullable: false),
                    IsDangerous = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "configuration",
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProvinceId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRateRules",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShippingRateCardId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginProvinceId = table.Column<long>(type: "bigint", nullable: true),
                    DestinationProvinceId = table.Column<long>(type: "bigint", nullable: true),
                    FromWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ToWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AdditionalPerKgRate = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SurchargePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinimumFee = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    EstimatedDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRateRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingRateRules_Provinces_DestinationProvinceId",
                        column: x => x.DestinationProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShippingRateRules_Provinces_OriginProvinceId",
                        column: x => x.OriginProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShippingRateRules_ShippingRateCards_ShippingRateCardId",
                        column: x => x.ShippingRateCardId,
                        principalSchema: "finance",
                        principalTable: "ShippingRateCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VendorBatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wards_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "configuration",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CountryId = table.Column<long>(type: "bigint", nullable: false),
                    ProvinceId = table.Column<long>(type: "bigint", nullable: true),
                    DistrictId = table.Column<long>(type: "bigint", nullable: true),
                    WardId = table.Column<long>(type: "bigint", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "configuration",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "configuration",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Addresses_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "configuration",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BranchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "crm",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "organization",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SchemaName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrimaryKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrierTrackingEvents",
                schema: "carrier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarrierId = table.Column<long>(type: "bigint", nullable: false),
                    CarrierTrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExternalEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessedShipmentId = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierTrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierTrackingEvents_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CODCollections",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    CollectedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CollectedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DriverReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CODCollections_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CODRemittanceItems",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CODRemittanceId = table.Column<long>(type: "bigint", nullable: false),
                    CODCollectionId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    CodAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DeductedShippingFee = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODRemittanceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CODRemittanceItems_CODCollections_CODCollectionId",
                        column: x => x.CODCollectionId,
                        principalSchema: "finance",
                        principalTable: "CODCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CODRemittances",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RemittanceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    RemittanceDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TotalCodAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalServiceFeesDeducted = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetRemittanceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RemittedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RemittedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CODRemittances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CODRemittances_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    AddressId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    AddressLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "crm",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBalances",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentOutstandingBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    UnbilledChargesAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalCodHoldingAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContracts",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    SignedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    FileAttachmentId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPricingRules",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerContractId = table.Column<long>(type: "bigint", nullable: true),
                    ShippingRateCardId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FlatDiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPricingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPricingRules_CustomerContracts_CustomerContractId",
                        column: x => x.CustomerContractId,
                        principalSchema: "crm",
                        principalTable: "CustomerContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerPricingRules_ShippingRateCards_ShippingRateCardId",
                        column: x => x.ShippingRateCardId,
                        principalSchema: "finance",
                        principalTable: "ShippingRateCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditTermDays = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountManagerEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialLedgerEntries",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialLedgerEntries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialLedgerEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false, computedColumnSql: "[TotalAmount] - [PaidAmount]"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedPickupDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiredDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    TotalVolumeCbm = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    CodAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderAddresses",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CountryId = table.Column<long>(type: "bigint", nullable: false),
                    ProvinceId = table.Column<long>(type: "bigint", nullable: true),
                    DistrictId = table.Column<long>(type: "bigint", nullable: true),
                    WardId = table.Column<long>(type: "bigint", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAddresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "configuration",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAddresses_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "configuration",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAddresses_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAddresses_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAddresses_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "configuration",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "configuration",
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderPackages",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PackageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VolumetricWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPackages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    OriginBranchId = table.Column<long>(type: "bigint", nullable: false),
                    DestinationBranchId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedCarrierId = table.Column<long>(type: "bigint", nullable: true),
                    CarrierServiceId = table.Column<long>(type: "bigint", nullable: true),
                    CarrierTrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShipmentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPackages = table.Column<int>(type: "int", nullable: false),
                    ActualWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    ChargeableWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CodAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    InsuranceFee = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SurchargesTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    PickupDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Branches_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Branches_OriginBranchId",
                        column: x => x.OriginBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_CarrierServices_CarrierServiceId",
                        column: x => x.CarrierServiceId,
                        principalSchema: "carrier",
                        principalTable: "CarrierServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Carriers_AssignedCarrierId",
                        column: x => x.AssignedCarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentAddresses",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CountryId = table.Column<long>(type: "bigint", nullable: false),
                    ProvinceId = table.Column<long>(type: "bigint", nullable: true),
                    DistrictId = table.Column<long>(type: "bigint", nullable: true),
                    WardId = table.Column<long>(type: "bigint", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FormattedAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAddresses_Countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "configuration",
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAddresses_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "configuration",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAddresses_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "configuration",
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAddresses_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentAddresses_Wards_WardId",
                        column: x => x.WardId,
                        principalSchema: "configuration",
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentCharges",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    ChargeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsInvoiced = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentCharges_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentCharges_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "finance",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentCharges_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentPackages",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    OrderPackageId = table.Column<long>(type: "bigint", nullable: true),
                    PackageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    VolumetricWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    PackageStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SealNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentPackages_OrderPackages_OrderPackageId",
                        column: x => x.OrderPackageId,
                        principalSchema: "sales",
                        principalTable: "OrderPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentPackages_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: true),
                    ShipmentChargeId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "finance",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_ShipmentCharges_ShipmentChargeId",
                        column: x => x.ShipmentChargeId,
                        principalSchema: "finance",
                        principalTable: "ShipmentCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAttempts",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    AttemptedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FailureNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextScheduledAttemptDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RecipientContacted = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryAttempts_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "organization",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    AddressId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "crm",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PickupRequests",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PickupCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedAddressId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedParcels = table.Column<int>(type: "int", nullable: false),
                    ActualParcelsPickedUp = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickupRequests_Addresses_RequestedAddressId",
                        column: x => x.RequestedAddressId,
                        principalSchema: "crm",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickupRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PickupRequests_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsLockedOut = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccounts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAreaSqM = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    AddressId = table.Column<long>(type: "bigint", nullable: true),
                    ManagerEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "crm",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warehouses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warehouses_Employees_ManagerEmployeeId",
                        column: x => x.ManagerEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachments_UserAccounts_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistories",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    UsernameAttempted = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistories_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<long>(type: "bigint", nullable: true),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RecipientPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "notification",
                        principalTable: "NotificationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_UserAccounts_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfirmedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "configuration",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_UserAccounts_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    NewReturnShipmentId = table.Column<long>(type: "bigint", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Shipments_NewReturnShipmentId",
                        column: x => x.NewReturnShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Shipments_OriginalShipmentId",
                        column: x => x.OriginalShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_UserAccounts_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentAssignments",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedToEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedBranchId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedVehicleId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_Branches_AssignedBranchId",
                        column: x => x.AssignedBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_UserAccounts_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserBranchAccess",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranchAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBranchAccess_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBranchAccess_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SessionTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    IsTerminated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ReorderPoint = table.Column<int>(type: "int", nullable: false),
                    SafetyStock = table.Column<int>(type: "int", nullable: false),
                    TargetStock = table.Column<int>(type: "int", nullable: false),
                    MinimumOrderQuantity = table.Column<int>(type: "int", nullable: false),
                    MaximumStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PerformedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Employees_PerformedByEmployeeId",
                        column: x => x.PerformedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParcelHandlingEvents",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentPackageId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BagNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NextDestinationWarehouseId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParcelHandlingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParcelHandlingEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelHandlingEvents_ShipmentPackages_ShipmentPackageId",
                        column: x => x.ShipmentPackageId,
                        principalSchema: "shipping",
                        principalTable: "ShipmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelHandlingEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelHandlingEvents_Warehouses_NextDestinationWarehouseId",
                        column: x => x.NextDestinationWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParcelHandlingEvents_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentRoutes",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    FromWarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ToWarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    TransportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CarrierId = table.Column<long>(type: "bigint", nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstimatedDeparture = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ActualDeparture = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    EstimatedArrival = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ActualArrival = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentRoutes_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShipmentRoutes_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentRoutes_Warehouses_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentRoutes_Warehouses_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentStatusHistories",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocationWarehouseId = table.Column<long>(type: "bigint", nullable: true),
                    LocationBranchId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedByEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_Branches_LocationBranchId",
                        column: x => x.LocationBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_Employees_ChangedByEmployeeId",
                        column: x => x.ChangedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_UserAccounts_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_Warehouses_LocationWarehouseId",
                        column: x => x.LocationWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdjustmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedByEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceWarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    DestinationWarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedByEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDispatches",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    DispatchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestinationWarehouseId = table.Column<long>(type: "bigint", nullable: true),
                    DestinationBranchId = table.Column<long>(type: "bigint", nullable: true),
                    AssignedCarrierId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DispatchedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatches_Branches_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatches_Carriers_AssignedCarrierId",
                        column: x => x.AssignedCarrierId,
                        principalSchema: "carrier",
                        principalTable: "Carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatches_Employees_DispatchedByEmployeeId",
                        column: x => x.DispatchedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatches_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseReceipts",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceBranchId = table.Column<long>(type: "bigint", nullable: true),
                    SourceVendor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpectedArrivalDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ActualArrivalDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseReceipts_Branches_SourceBranchId",
                        column: x => x.SourceBranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseReceipts_Employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseZones",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ZoneType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemperatureType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseZones_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProofOfDeliveries",
                schema: "shipping",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    DeliveryAttemptId = table.Column<long>(type: "bigint", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RecipientRelation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientNationalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SignatureAttachmentId = table.Column<long>(type: "bigint", nullable: true),
                    PhotoAttachmentId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProofOfDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_DeliveryAttempts_DeliveryAttemptId",
                        column: x => x.DeliveryAttemptId,
                        principalSchema: "shipping",
                        principalTable: "DeliveryAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_FileAttachments_PhotoAttachmentId",
                        column: x => x.PhotoAttachmentId,
                        principalSchema: "audit",
                        principalTable: "FileAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_FileAttachments_SignatureAttachmentId",
                        column: x => x.SignatureAttachmentId,
                        principalSchema: "audit",
                        principalTable: "FileAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "shipping",
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProofOfDeliveries_UserAccounts_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GatewayResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "notification",
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    AllocatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "finance",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "finance",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_UserAccounts_AllocatedByUserId",
                        column: x => x.AllocatedByUserId,
                        principalSchema: "identity",
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDispatchPackages",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseDispatchId = table.Column<long>(type: "bigint", nullable: false),
                    ShipmentPackageId = table.Column<long>(type: "bigint", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ScannedByEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    TargetContainerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDispatchPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchPackages_Employees_ScannedByEmployeeId",
                        column: x => x.ScannedByEmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchPackages_ShipmentPackages_ShipmentPackageId",
                        column: x => x.ShipmentPackageId,
                        principalSchema: "shipping",
                        principalTable: "ShipmentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchPackages_WarehouseDispatches_WarehouseDispatchId",
                        column: x => x.WarehouseDispatchId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseZoneId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aisle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Rack = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Shelf = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Bin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    MaxVolumeCbm = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseLocations_WarehouseZones_WarehouseZoneId",
                        column: x => x.WarehouseZoneId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseLocationId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LotId = table.Column<long>(type: "bigint", nullable: true),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    QuantityAllocated = table.Column<int>(type: "int", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "int", nullable: false, computedColumnSql: "[QuantityOnHand] - [QuantityAllocated]"),
                    QuantityDamaged = table.Column<int>(type: "int", nullable: false),
                    LastCountDate = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_InventoryLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_WarehouseLocations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    LotId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_InventoryLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventorySerialNumbers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySerialNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySerialNumbers_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySerialNumbers_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventorySerialNumbers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactionItems",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    LotId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionItems_InventoryLots_LotId",
                        column: x => x.LotId,
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionItems_InventoryTransactions_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalSchema: "inventory",
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactionItems_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDispatchItems",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseDispatchId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    PickedQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false),
                    SourceLocationId = table.Column<long>(type: "bigint", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchItems_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "configuration",
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchItems_WarehouseDispatches_WarehouseDispatchId",
                        column: x => x.WarehouseDispatchId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseDispatchItems_WarehouseLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseReceiptItems",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseReceiptId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                    DamagedQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasureId = table.Column<long>(type: "bigint", nullable: false),
                    TargetLocationId = table.Column<long>(type: "bigint", nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseReceiptItems_UnitsOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "configuration",
                        principalTable: "UnitsOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseReceiptItems_WarehouseLocations_TargetLocationId",
                        column: x => x.TargetLocationId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarehouseReceiptItems_WarehouseReceipts_WarehouseReceiptId",
                        column: x => x.WarehouseReceiptId,
                        principalSchema: "warehouse",
                        principalTable: "WarehouseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CountryId",
                schema: "crm",
                table: "Addresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_DistrictId",
                schema: "crm",
                table: "Addresses",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ProvinceId",
                schema: "crm",
                table: "Addresses",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_PublicId",
                schema: "crm",
                table: "Addresses",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_WardId",
                schema: "crm",
                table: "Addresses",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PublicId",
                schema: "audit",
                table: "AuditLogs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "audit",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_JobName",
                schema: "integration",
                table: "BackgroundJobLogs",
                column: "JobName");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_AddressId",
                schema: "organization",
                table: "Branches",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                schema: "organization",
                table: "Branches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId",
                schema: "organization",
                table: "Branches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_PublicId",
                schema: "organization",
                table: "Branches",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierApiLogs_CarrierId",
                schema: "carrier",
                table: "CarrierApiLogs",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierConfigurations_CarrierId",
                schema: "carrier",
                table: "CarrierConfigurations",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_Code",
                schema: "carrier",
                table: "Carriers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_PublicId",
                schema: "carrier",
                table: "Carriers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierServices_CarrierId",
                schema: "carrier",
                table: "CarrierServices",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierServices_PublicId",
                schema: "carrier",
                table: "CarrierServices",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierSyncJobs_CarrierId",
                schema: "carrier",
                table: "CarrierSyncJobs",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierTrackingEvents_CarrierId",
                schema: "carrier",
                table: "CarrierTrackingEvents",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierTrackingEvents_ProcessedShipmentId",
                schema: "carrier",
                table: "CarrierTrackingEvents",
                column: "ProcessedShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierTrackingMappings_CarrierId",
                schema: "carrier",
                table: "CarrierTrackingMappings",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CarrierWebhookLogs_CarrierId",
                schema: "carrier",
                table: "CarrierWebhookLogs",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_CODCollections_CollectedByEmployeeId",
                schema: "finance",
                table: "CODCollections",
                column: "CollectedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CODCollections_CurrencyId",
                schema: "finance",
                table: "CODCollections",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CODCollections_PublicId",
                schema: "finance",
                table: "CODCollections",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CODCollections_ShipmentId",
                schema: "finance",
                table: "CODCollections",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CODCollections_VerifiedByUserId",
                schema: "finance",
                table: "CODCollections",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittanceItems_CODCollectionId",
                schema: "finance",
                table: "CODRemittanceItems",
                column: "CODCollectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittanceItems_CODRemittanceId",
                schema: "finance",
                table: "CODRemittanceItems",
                column: "CODRemittanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittanceItems_ShipmentId",
                schema: "finance",
                table: "CODRemittanceItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittances_CurrencyId",
                schema: "finance",
                table: "CODRemittances",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittances_CustomerId",
                schema: "finance",
                table: "CODRemittances",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittances_PublicId",
                schema: "finance",
                table: "CODRemittances",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittances_RemittanceNumber",
                schema: "finance",
                table: "CODRemittances",
                column: "RemittanceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CODRemittances_RemittedByUserId",
                schema: "finance",
                table: "CODRemittances",
                column: "RemittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                schema: "organization",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PublicId",
                schema: "organization",
                table: "Companies",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TaxNumber",
                schema: "organization",
                table: "Companies",
                column: "TaxNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Alpha3Code",
                schema: "configuration",
                table: "Countries",
                column: "Alpha3Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                schema: "configuration",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                schema: "configuration",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_AddressId",
                schema: "crm",
                table: "CustomerAddresses",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                schema: "crm",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_PublicId",
                schema: "crm",
                table: "CustomerAddresses",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalances_CustomerId",
                schema: "finance",
                table: "CustomerBalances",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                schema: "crm",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_PublicId",
                schema: "crm",
                table: "CustomerContacts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_ContractNumber",
                schema: "crm",
                table: "CustomerContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_CustomerId",
                schema: "crm",
                table: "CustomerContracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_FileAttachmentId",
                schema: "crm",
                table: "CustomerContracts",
                column: "FileAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_PublicId",
                schema: "crm",
                table: "CustomerContracts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingRules_CustomerContractId",
                schema: "crm",
                table: "CustomerPricingRules",
                column: "CustomerContractId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingRules_CustomerId",
                schema: "crm",
                table: "CustomerPricingRules",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingRules_PublicId",
                schema: "crm",
                table: "CustomerPricingRules",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPricingRules_ShippingRateCardId",
                schema: "crm",
                table: "CustomerPricingRules",
                column: "ShippingRateCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountManagerEmployeeId",
                schema: "crm",
                table: "Customers",
                column: "AccountManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId",
                schema: "crm",
                table: "Customers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                schema: "crm",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PublicId",
                schema: "crm",
                table: "Customers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_AttemptedByEmployeeId",
                schema: "shipping",
                table: "DeliveryAttempts",
                column: "AttemptedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAttempts_ShipmentId",
                schema: "shipping",
                table: "DeliveryAttempts",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId",
                schema: "organization",
                table: "Departments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                schema: "organization",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerEmployeeId",
                schema: "organization",
                table: "Departments",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_PublicId",
                schema: "organization",
                table: "Departments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Code",
                schema: "configuration",
                table: "Districts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_ProvinceId",
                schema: "configuration",
                table: "Districts",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AddressId",
                schema: "organization",
                table: "Employees",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId",
                schema: "organization",
                table: "Employees",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Code",
                schema: "organization",
                table: "Employees",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                schema: "organization",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PublicId",
                schema: "organization",
                table: "Employees",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_EntityType_EntityId",
                schema: "audit",
                table: "FileAttachments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_PublicId",
                schema: "audit",
                table: "FileAttachments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_UploadedByUserId",
                schema: "audit",
                table: "FileAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_CurrencyId",
                schema: "finance",
                table: "FinancialLedgerEntries",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_CustomerId",
                schema: "finance",
                table: "FinancialLedgerEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_EntryNumber",
                schema: "finance",
                table: "FinancialLedgerEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_PublicId",
                schema: "finance",
                table: "FinancialLedgerEntries",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_LotId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_ProductId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseId_WarehouseLocationId_ProductId_LotId",
                schema: "inventory",
                table: "InventoryBalances",
                columns: new[] { "WarehouseId", "WarehouseLocationId", "ProductId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBalances_WarehouseLocationId",
                schema: "inventory",
                table: "InventoryBalances",
                column: "WarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId_WarehouseId",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_PublicId",
                schema: "inventory",
                table: "InventoryItems",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_WarehouseId",
                schema: "inventory",
                table: "InventoryItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_ProductId_LotNumber",
                schema: "inventory",
                table: "InventoryLots",
                columns: new[] { "ProductId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_PublicId",
                schema: "inventory",
                table: "InventoryLots",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_LocationId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_LotId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_ProductId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_PublicId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_WarehouseId",
                schema: "inventory",
                table: "InventoryReservations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialNumbers_LocationId",
                schema: "inventory",
                table: "InventorySerialNumbers",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialNumbers_ProductId_SerialNumber",
                schema: "inventory",
                table: "InventorySerialNumbers",
                columns: new[] { "ProductId", "SerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventorySerialNumbers_WarehouseId",
                schema: "inventory",
                table: "InventorySerialNumbers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionItems_InventoryTransactionId",
                schema: "inventory",
                table: "InventoryTransactionItems",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionItems_LocationId",
                schema: "inventory",
                table: "InventoryTransactionItems",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionItems_LotId",
                schema: "inventory",
                table: "InventoryTransactionItems",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactionItems_ProductId",
                schema: "inventory",
                table: "InventoryTransactionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PerformedByEmployeeId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "PerformedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PublicId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TransactionNumber",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                schema: "inventory",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                schema: "finance",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ShipmentChargeId",
                schema: "finance",
                table: "InvoiceItems",
                column: "ShipmentChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_ShipmentId",
                schema: "finance",
                table: "InvoiceItems",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "finance",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                schema: "finance",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PublicId",
                schema: "finance",
                table: "Invoices",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId",
                schema: "identity",
                table: "LoginHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                schema: "notification",
                table: "NotificationDeliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PublicId",
                schema: "notification",
                table: "Notifications",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                schema: "notification",
                table: "Notifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TemplateId",
                schema: "notification",
                table: "Notifications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_Code",
                schema: "notification",
                table: "NotificationTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_PublicId",
                schema: "notification",
                table: "NotificationTemplates",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Scope",
                schema: "configuration",
                table: "NumberSequences",
                column: "Scope",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_CountryId",
                schema: "sales",
                table: "OrderAddresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_DistrictId",
                schema: "sales",
                table: "OrderAddresses",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_OrderId",
                schema: "sales",
                table: "OrderAddresses",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_ProvinceId",
                schema: "sales",
                table: "OrderAddresses",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_WardId",
                schema: "sales",
                table: "OrderAddresses",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "sales",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                schema: "sales",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PublicId",
                schema: "sales",
                table: "OrderItems",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_UnitOfMeasureId",
                schema: "sales",
                table: "OrderItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_OrderId",
                schema: "sales",
                table: "OrderPackages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_PublicId",
                schema: "sales",
                table: "OrderPackages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId",
                schema: "sales",
                table: "Orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CurrencyId",
                schema: "sales",
                table: "Orders",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "sales",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                schema: "sales",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PublicId",
                schema: "sales",
                table: "Orders",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EventType",
                schema: "integration",
                table: "OutboxMessages",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status",
                schema: "integration",
                table: "OutboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelHandlingEvents_EmployeeId",
                schema: "warehouse",
                table: "ParcelHandlingEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelHandlingEvents_NextDestinationWarehouseId",
                schema: "warehouse",
                table: "ParcelHandlingEvents",
                column: "NextDestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelHandlingEvents_ShipmentId",
                schema: "warehouse",
                table: "ParcelHandlingEvents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelHandlingEvents_ShipmentPackageId",
                schema: "warehouse",
                table: "ParcelHandlingEvents",
                column: "ShipmentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ParcelHandlingEvents_WarehouseId",
                schema: "warehouse",
                table: "ParcelHandlingEvents",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_AllocatedByUserId",
                schema: "finance",
                table: "PaymentAllocations",
                column: "AllocatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                schema: "finance",
                table: "PaymentAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId",
                schema: "finance",
                table: "PaymentAllocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ConfirmedByUserId",
                schema: "finance",
                table: "Payments",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CurrencyId",
                schema: "finance",
                table: "Payments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                schema: "finance",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentNumber",
                schema: "finance",
                table: "Payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PublicId",
                schema: "finance",
                table: "Payments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "identity",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PublicId",
                schema: "identity",
                table: "Permissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupRequests_AssignedEmployeeId",
                schema: "shipping",
                table: "PickupRequests",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupRequests_CustomerId",
                schema: "shipping",
                table: "PickupRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupRequests_PickupCode",
                schema: "shipping",
                table: "PickupRequests",
                column: "PickupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupRequests_PublicId",
                schema: "shipping",
                table: "PickupRequests",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupRequests_RequestedAddressId",
                schema: "shipping",
                table: "PickupRequests",
                column: "RequestedAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Code",
                schema: "catalog",
                table: "ProductCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                schema: "catalog",
                table: "ProductCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_PublicId",
                schema: "catalog",
                table: "ProductCategories",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                schema: "catalog",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                schema: "catalog",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CurrencyId",
                schema: "catalog",
                table: "Products",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PublicId",
                schema: "catalog",
                table: "Products",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                schema: "catalog",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitOfMeasureId",
                schema: "catalog",
                table: "Products",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_DeliveryAttemptId",
                schema: "shipping",
                table: "ProofOfDeliveries",
                column: "DeliveryAttemptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_PhotoAttachmentId",
                schema: "shipping",
                table: "ProofOfDeliveries",
                column: "PhotoAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_ShipmentId",
                schema: "shipping",
                table: "ProofOfDeliveries",
                column: "ShipmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_SignatureAttachmentId",
                schema: "shipping",
                table: "ProofOfDeliveries",
                column: "SignatureAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfDeliveries_VerifiedByUserId",
                schema: "shipping",
                table: "ProofOfDeliveries",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Code",
                schema: "configuration",
                table: "Provinces",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_CountryId",
                schema: "configuration",
                table: "Provinces",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "identity",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "identity",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_Code",
                schema: "notification",
                table: "ReportDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_PublicId",
                schema: "notification",
                table: "ReportDefinitions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_ApprovedByUserId",
                schema: "shipping",
                table: "ReturnRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_NewReturnShipmentId",
                schema: "shipping",
                table: "ReturnRequests",
                column: "NewReturnShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OriginalShipmentId",
                schema: "shipping",
                table: "ReturnRequests",
                column: "OriginalShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_PublicId",
                schema: "shipping",
                table: "ReturnRequests",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_ReturnCode",
                schema: "shipping",
                table: "ReturnRequests",
                column: "ReturnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                schema: "identity",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_PublicId",
                schema: "identity",
                table: "Roles",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_PublicId",
                schema: "notification",
                table: "ScheduledReports",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_ReportDefinitionId",
                schema: "notification",
                table: "ScheduledReports",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddresses_CountryId",
                schema: "shipping",
                table: "ShipmentAddresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddresses_DistrictId",
                schema: "shipping",
                table: "ShipmentAddresses",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddresses_ProvinceId",
                schema: "shipping",
                table: "ShipmentAddresses",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddresses_ShipmentId",
                schema: "shipping",
                table: "ShipmentAddresses",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddresses_WardId",
                schema: "shipping",
                table: "ShipmentAddresses",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_AssignedBranchId",
                schema: "shipping",
                table: "ShipmentAssignments",
                column: "AssignedBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_AssignedByUserId",
                schema: "shipping",
                table: "ShipmentAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_AssignedToEmployeeId",
                schema: "shipping",
                table: "ShipmentAssignments",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_ShipmentId",
                schema: "shipping",
                table: "ShipmentAssignments",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCharges_CurrencyId",
                schema: "finance",
                table: "ShipmentCharges",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCharges_InvoiceId",
                schema: "finance",
                table: "ShipmentCharges",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCharges_ShipmentId",
                schema: "finance",
                table: "ShipmentCharges",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPackages_Barcode",
                schema: "shipping",
                table: "ShipmentPackages",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPackages_OrderPackageId",
                schema: "shipping",
                table: "ShipmentPackages",
                column: "OrderPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPackages_PublicId",
                schema: "shipping",
                table: "ShipmentPackages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentPackages_ShipmentId",
                schema: "shipping",
                table: "ShipmentPackages",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoutes_CarrierId",
                schema: "shipping",
                table: "ShipmentRoutes",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoutes_FromWarehouseId",
                schema: "shipping",
                table: "ShipmentRoutes",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoutes_ShipmentId",
                schema: "shipping",
                table: "ShipmentRoutes",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRoutes_ToWarehouseId",
                schema: "shipping",
                table: "ShipmentRoutes",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedCarrierId",
                schema: "shipping",
                table: "Shipments",
                column: "AssignedCarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierServiceId",
                schema: "shipping",
                table: "Shipments",
                column: "CarrierServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierTrackingNumber",
                schema: "shipping",
                table: "Shipments",
                column: "CarrierTrackingNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CurrencyId",
                schema: "shipping",
                table: "Shipments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CustomerId",
                schema: "shipping",
                table: "Shipments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DestinationBranchId",
                schema: "shipping",
                table: "Shipments",
                column: "DestinationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                schema: "shipping",
                table: "Shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OriginBranchId",
                schema: "shipping",
                table: "Shipments",
                column: "OriginBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_PublicId",
                schema: "shipping",
                table: "Shipments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TrackingNumber",
                schema: "shipping",
                table: "Shipments",
                column: "TrackingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_ChangedByEmployeeId",
                schema: "shipping",
                table: "ShipmentStatusHistories",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_ChangedByUserId",
                schema: "shipping",
                table: "ShipmentStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_LocationBranchId",
                schema: "shipping",
                table: "ShipmentStatusHistories",
                column: "LocationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_LocationWarehouseId",
                schema: "shipping",
                table: "ShipmentStatusHistories",
                column: "LocationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_ShipmentId",
                schema: "shipping",
                table: "ShipmentStatusHistories",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateCards_BaseCurrencyId",
                schema: "finance",
                table: "ShippingRateCards",
                column: "BaseCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateCards_Code",
                schema: "finance",
                table: "ShippingRateCards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateCards_PublicId",
                schema: "finance",
                table: "ShippingRateCards",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateRules_DestinationProvinceId",
                schema: "finance",
                table: "ShippingRateRules",
                column: "DestinationProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateRules_OriginProvinceId",
                schema: "finance",
                table: "ShippingRateRules",
                column: "OriginProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRateRules_ShippingRateCardId",
                schema: "finance",
                table: "ShippingRateRules",
                column: "ShippingRateCardId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_AdjustmentNumber",
                schema: "inventory",
                table: "StockAdjustments",
                column: "AdjustmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ApprovedByEmployeeId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_PublicId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_WarehouseId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ApprovedByEmployeeId",
                schema: "inventory",
                table: "StockTransfers",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationWarehouseId",
                schema: "inventory",
                table: "StockTransfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_PublicId",
                schema: "inventory",
                table: "StockTransfers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_RequestedByEmployeeId",
                schema: "inventory",
                table: "StockTransfers",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceWarehouseId",
                schema: "inventory",
                table: "StockTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNumber",
                schema: "inventory",
                table: "StockTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                schema: "configuration",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Code",
                schema: "configuration",
                table: "UnitsOfMeasure",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                schema: "identity",
                table: "UserAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_EmployeeId",
                schema: "identity",
                table: "UserAccounts",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_PublicId",
                schema: "identity",
                table: "UserAccounts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Username",
                schema: "identity",
                table: "UserAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchAccess_BranchId",
                schema: "identity",
                table: "UserBranchAccess",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchAccess_UserId_BranchId",
                schema: "identity",
                table: "UserBranchAccess",
                columns: new[] { "UserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                schema: "identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionTokenHash",
                schema: "identity",
                table: "UserSessions",
                column: "SessionTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                schema: "identity",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_Code",
                schema: "configuration",
                table: "Wards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DistrictId",
                schema: "configuration",
                table: "Wards",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_AssignedCarrierId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "AssignedCarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_DestinationBranchId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "DestinationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_DestinationWarehouseId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_DispatchedByEmployeeId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "DispatchedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_DispatchNumber",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "DispatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_PublicId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatches_WarehouseId",
                schema: "warehouse",
                table: "WarehouseDispatches",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchItems_ProductId",
                schema: "warehouse",
                table: "WarehouseDispatchItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchItems_SourceLocationId",
                schema: "warehouse",
                table: "WarehouseDispatchItems",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchItems_UnitOfMeasureId",
                schema: "warehouse",
                table: "WarehouseDispatchItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchItems_WarehouseDispatchId",
                schema: "warehouse",
                table: "WarehouseDispatchItems",
                column: "WarehouseDispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchPackages_ScannedByEmployeeId",
                schema: "warehouse",
                table: "WarehouseDispatchPackages",
                column: "ScannedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchPackages_ShipmentPackageId",
                schema: "warehouse",
                table: "WarehouseDispatchPackages",
                column: "ShipmentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDispatchPackages_WarehouseDispatchId",
                schema: "warehouse",
                table: "WarehouseDispatchPackages",
                column: "WarehouseDispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_Barcode",
                schema: "warehouse",
                table: "WarehouseLocations",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_PublicId",
                schema: "warehouse",
                table: "WarehouseLocations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_WarehouseZoneId",
                schema: "warehouse",
                table: "WarehouseLocations",
                column: "WarehouseZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceiptItems_ProductId",
                schema: "warehouse",
                table: "WarehouseReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceiptItems_TargetLocationId",
                schema: "warehouse",
                table: "WarehouseReceiptItems",
                column: "TargetLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceiptItems_UnitOfMeasureId",
                schema: "warehouse",
                table: "WarehouseReceiptItems",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceiptItems_WarehouseReceiptId",
                schema: "warehouse",
                table: "WarehouseReceiptItems",
                column: "WarehouseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_PublicId",
                schema: "warehouse",
                table: "WarehouseReceipts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_ReceiptNumber",
                schema: "warehouse",
                table: "WarehouseReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_ReceivedByEmployeeId",
                schema: "warehouse",
                table: "WarehouseReceipts",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_SourceBranchId",
                schema: "warehouse",
                table: "WarehouseReceipts",
                column: "SourceBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseReceipts_WarehouseId",
                schema: "warehouse",
                table: "WarehouseReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_AddressId",
                schema: "organization",
                table: "Warehouses",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                schema: "organization",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                schema: "organization",
                table: "Warehouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_ManagerEmployeeId",
                schema: "organization",
                table: "Warehouses",
                column: "ManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_PublicId",
                schema: "organization",
                table: "Warehouses",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_PublicId",
                schema: "warehouse",
                table: "WarehouseZones",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseZones_WarehouseId",
                schema: "warehouse",
                table: "WarehouseZones",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_UserAccounts_UserId",
                schema: "audit",
                table: "AuditLogs",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CarrierTrackingEvents_Shipments_ProcessedShipmentId",
                schema: "carrier",
                table: "CarrierTrackingEvents",
                column: "ProcessedShipmentId",
                principalSchema: "shipping",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CODCollections_Employees_CollectedByEmployeeId",
                schema: "finance",
                table: "CODCollections",
                column: "CollectedByEmployeeId",
                principalSchema: "organization",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CODCollections_Shipments_ShipmentId",
                schema: "finance",
                table: "CODCollections",
                column: "ShipmentId",
                principalSchema: "shipping",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CODCollections_UserAccounts_VerifiedByUserId",
                schema: "finance",
                table: "CODCollections",
                column: "VerifiedByUserId",
                principalSchema: "identity",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CODRemittanceItems_CODRemittances_CODRemittanceId",
                schema: "finance",
                table: "CODRemittanceItems",
                column: "CODRemittanceId",
                principalSchema: "finance",
                principalTable: "CODRemittances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CODRemittanceItems_Shipments_ShipmentId",
                schema: "finance",
                table: "CODRemittanceItems",
                column: "ShipmentId",
                principalSchema: "shipping",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CODRemittances_Customers_CustomerId",
                schema: "finance",
                table: "CODRemittances",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CODRemittances_UserAccounts_RemittedByUserId",
                schema: "finance",
                table: "CODRemittances",
                column: "RemittedByUserId",
                principalSchema: "identity",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAddresses_Customers_CustomerId",
                schema: "crm",
                table: "CustomerAddresses",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerBalances_Customers_CustomerId",
                schema: "finance",
                table: "CustomerBalances",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerContacts_Customers_CustomerId",
                schema: "crm",
                table: "CustomerContacts",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerContracts_Customers_CustomerId",
                schema: "crm",
                table: "CustomerContracts",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerContracts_FileAttachments_FileAttachmentId",
                schema: "crm",
                table: "CustomerContracts",
                column: "FileAttachmentId",
                principalSchema: "audit",
                principalTable: "FileAttachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPricingRules_Customers_CustomerId",
                schema: "crm",
                table: "CustomerPricingRules",
                column: "CustomerId",
                principalSchema: "crm",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Employees_AccountManagerEmployeeId",
                schema: "crm",
                table: "Customers",
                column: "AccountManagerEmployeeId",
                principalSchema: "organization",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryAttempts_Employees_AttemptedByEmployeeId",
                schema: "shipping",
                table: "DeliveryAttempts",
                column: "AttemptedByEmployeeId",
                principalSchema: "organization",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_ManagerEmployeeId",
                schema: "organization",
                table: "Departments",
                column: "ManagerEmployeeId",
                principalSchema: "organization",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Countries_CountryId",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Provinces_Countries_CountryId",
                schema: "configuration",
                table: "Provinces");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Districts_DistrictId",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_Districts_DistrictId",
                schema: "configuration",
                table: "Wards");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Provinces_ProvinceId",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Wards_WardId",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Addresses_AddressId",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Addresses_AddressId",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Companies_CompanyId",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Companies_CompanyId",
                schema: "organization",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_ManagerEmployeeId",
                schema: "organization",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "BackgroundJobLogs",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "CarrierApiLogs",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CarrierConfigurations",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CarrierSyncJobs",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CarrierTrackingEvents",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CarrierTrackingMappings",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CarrierWebhookLogs",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "CODRemittanceItems",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "CustomerBalances",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "CustomerPricingRules",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "FinancialLedgerEntries",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "InventoryBalances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryItems",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryReservations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventorySerialNumbers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryTransactionItems",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InvoiceItems",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "LoginHistories",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "NumberSequences",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "OrderAddresses",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "ParcelHandlingEvents",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PickupRequests",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "ProofOfDeliveries",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ReturnRequests",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ScheduledReports",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "ShipmentAddresses",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "ShipmentAssignments",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "ShipmentRoutes",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "ShipmentStatusHistories",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "ShippingRateRules",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "StockAdjustments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "SystemSettings",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "UserBranchAccess",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "WarehouseDispatchItems",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "WarehouseDispatchPackages",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "WarehouseReceiptItems",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "CODCollections",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "CODRemittances",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "CustomerContracts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "InventoryLots",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ShipmentCharges",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DeliveryAttempts",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ReportDefinitions",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "ShippingRateCards",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ShipmentPackages",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "WarehouseDispatches",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "WarehouseLocations",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "WarehouseReceipts",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "FileAttachments",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "NotificationTemplates",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "OrderPackages",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Shipments",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "WarehouseZones",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "UserAccounts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ProductCategories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "CarrierServices",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Carriers",
                schema: "carrier");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "Districts",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "Provinces",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "Wards",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Branches",
                schema: "organization");
        }
    }
}
