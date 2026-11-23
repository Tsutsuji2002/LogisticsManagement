IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'crm') IS NULL EXEC(N'CREATE SCHEMA [crm];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'audit') IS NULL EXEC(N'CREATE SCHEMA [audit];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'integration') IS NULL EXEC(N'CREATE SCHEMA [integration];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'organization') IS NULL EXEC(N'CREATE SCHEMA [organization];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'carrier') IS NULL EXEC(N'CREATE SCHEMA [carrier];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'finance') IS NULL EXEC(N'CREATE SCHEMA [finance];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'configuration') IS NULL EXEC(N'CREATE SCHEMA [configuration];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'shipping') IS NULL EXEC(N'CREATE SCHEMA [shipping];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'inventory') IS NULL EXEC(N'CREATE SCHEMA [inventory];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'notification') IS NULL EXEC(N'CREATE SCHEMA [notification];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'sales') IS NULL EXEC(N'CREATE SCHEMA [sales];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'warehouse') IS NULL EXEC(N'CREATE SCHEMA [warehouse];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    IF SCHEMA_ID(N'catalog') IS NULL EXEC(N'CREATE SCHEMA [catalog];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [integration].[BackgroundJobLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [JobName] nvarchar(100) NOT NULL,
        [ExecutionId] nvarchar(100) NOT NULL,
        [TriggerType] nvarchar(50) NOT NULL,
        [StartedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CompletedAt] datetime2(7) NULL,
        [Status] nvarchar(50) NOT NULL,
        [RecordsProcessed] int NOT NULL,
        [RecordsSucceeded] int NOT NULL,
        [RecordsFailed] int NOT NULL,
        [LogDetails] nvarchar(max) NULL,
        [ExceptionDetails] nvarchar(max) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BackgroundJobLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[Carriers] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [ContactPhone] nvarchar(50) NULL,
        [ContactEmail] nvarchar(150) NULL,
        [WebsiteUrl] nvarchar(250) NULL,
        [ApiBaseUrl] nvarchar(250) NOT NULL,
        [TrackingUrlTemplate] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Carriers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [organization].[Companies] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [LegalName] nvarchar(200) NOT NULL,
        [TradingName] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [Website] nvarchar(200) NULL,
        [LogoUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[Countries] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(2) NOT NULL,
        [Alpha3Code] nvarchar(3) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [NativeName] nvarchar(100) NULL,
        [NumericCode] int NULL,
        [PhonePrefix] nvarchar(10) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Countries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[Currencies] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(3) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Symbol] nvarchar(10) NOT NULL,
        [DecimalPlaces] int NOT NULL,
        [ExchangeRateToDefault] decimal(18,6) NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [notification].[NotificationTemplates] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Channel] nvarchar(50) NOT NULL,
        [SubjectTemplate] nvarchar(500) NULL,
        [BodyTemplate] nvarchar(max) NOT NULL,
        [ParametersJson] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[NumberSequences] (
        [Id] bigint NOT NULL IDENTITY,
        [Scope] nvarchar(50) NOT NULL,
        [Prefix] nvarchar(20) NOT NULL,
        [Suffix] nvarchar(20) NULL,
        [NextNumber] bigint NOT NULL,
        [Increment] int NOT NULL,
        [NumberLength] int NOT NULL,
        [CurrentFormat] nvarchar(100) NOT NULL,
        [ResetFrequency] nvarchar(20) NOT NULL,
        [LastResetDate] date NULL,
        CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [integration].[OutboxMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [EventType] nvarchar(200) NOT NULL,
        [AggregateType] nvarchar(100) NOT NULL,
        [AggregateId] nvarchar(100) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ProcessedAt] datetime2(7) NULL,
        [Status] nvarchar(50) NOT NULL,
        [RetryCount] int NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[Permissions] (
        [Id] bigint NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Module] nvarchar(50) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [catalog].[ProductCategories] (
        [Id] bigint NOT NULL IDENTITY,
        [ParentCategoryId] bigint NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductCategories_ProductCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [catalog].[ProductCategories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [notification].[ReportDefinitions] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [QueryDefinition] nvarchar(max) NOT NULL,
        [ParametersSchemaJson] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[Roles] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[SystemSettings] (
        [Id] bigint NOT NULL IDENTITY,
        [Category] nvarchar(50) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ValueType] nvarchar(50) NOT NULL,
        [IsEncrypted] bit NOT NULL,
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[UnitsOfMeasure] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [ConversionFactor] decimal(18,6) NOT NULL,
        [IsBaseUnit] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_UnitsOfMeasure] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierApiLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [Endpoint] nvarchar(500) NOT NULL,
        [HttpMethod] nvarchar(10) NOT NULL,
        [RequestPayloadMasked] nvarchar(max) NULL,
        [ResponsePayloadMasked] nvarchar(max) NULL,
        [ResponseStatusCode] int NOT NULL,
        [DurationMs] int NOT NULL,
        [Timestamp] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsSuccess] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CarrierApiLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierApiLogs_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierConfigurations] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [Environment] nvarchar(50) NOT NULL,
        [ApiKey] nvarchar(500) NOT NULL,
        [ApiSecret] nvarchar(500) NULL,
        [ClientId] nvarchar(100) NULL,
        [Username] nvarchar(100) NULL,
        [EncryptedPassword] nvarchar(500) NULL,
        [WebhookSecret] nvarchar(500) NULL,
        [SettingsJson] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CarrierConfigurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierConfigurations_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierServices] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [ServiceCode] nvarchar(50) NOT NULL,
        [ServiceName] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [EstimatedTransitHours] int NULL,
        [MaxWeightKg] decimal(12,3) NULL,
        [MaxLengthCm] decimal(12,2) NULL,
        [IsCodSupported] bit NOT NULL,
        [IsInsuranceSupported] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CarrierServices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierServices_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierSyncJobs] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [JobType] nvarchar(50) NOT NULL,
        [StartTime] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [EndTime] datetime2(7) NULL,
        [Status] nvarchar(50) NOT NULL,
        [TotalRecordsProcessed] int NOT NULL,
        [TotalSuccessCount] int NOT NULL,
        [TotalFailureCount] int NOT NULL,
        [ErrorLog] nvarchar(max) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CarrierSyncJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierSyncJobs_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierTrackingMappings] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [ExternalStatusCode] nvarchar(100) NOT NULL,
        [ExternalStatusName] nvarchar(150) NULL,
        [InternalShipmentStatus] nvarchar(50) NOT NULL,
        [InternalSubStatus] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_CarrierTrackingMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierTrackingMappings_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierWebhookLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [ExternalEventId] nvarchar(100) NULL,
        [WebhookUrl] nvarchar(500) NOT NULL,
        [HttpMethod] nvarchar(10) NOT NULL,
        [HeadersJson] nvarchar(max) NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ResponseStatusCode] int NOT NULL,
        [ResponseBody] nvarchar(max) NULL,
        [ProcessedSuccessfully] bit NOT NULL,
        [ProcessedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ErrorDetails] nvarchar(max) NULL,
        [ClientIpAddress] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_CarrierWebhookLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierWebhookLogs_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[Provinces] (
        [Id] bigint NOT NULL IDENTITY,
        [CountryId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Provinces_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [configuration].[Countries] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[ShippingRateCards] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [BaseCurrencyId] bigint NOT NULL,
        [EffectiveFrom] datetime2(7) NOT NULL,
        [EffectiveTo] datetime2(7) NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ShippingRateCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShippingRateCards_Currencies_BaseCurrencyId] FOREIGN KEY ([BaseCurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [notification].[ScheduledReports] (
        [Id] bigint NOT NULL IDENTITY,
        [ReportDefinitionId] bigint NOT NULL,
        [ScheduleCron] nvarchar(100) NOT NULL,
        [OutputFormat] nvarchar(20) NOT NULL,
        [RecipientEmails] nvarchar(500) NOT NULL,
        [LastRunAt] datetime2(7) NULL,
        [NextRunAt] datetime2(7) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ScheduledReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduledReports_ReportDefinitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [notification].[ReportDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[RolePermissions] (
        [Id] bigint NOT NULL IDENTITY,
        [RoleId] bigint NOT NULL,
        [PermissionId] bigint NOT NULL,
        [GrantedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [GrantedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [identity].[Permissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [catalog].[Products] (
        [Id] bigint NOT NULL IDENTITY,
        [CategoryId] bigint NOT NULL,
        [Sku] nvarchar(50) NOT NULL,
        [Barcode] nvarchar(100) NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [UnitOfMeasureId] bigint NOT NULL,
        [WeightKg] decimal(12,3) NOT NULL,
        [LengthCm] decimal(12,2) NOT NULL,
        [WidthCm] decimal(12,2) NOT NULL,
        [HeightCm] decimal(12,2) NOT NULL,
        [DeclaredValue] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [IsPerishable] bit NOT NULL,
        [IsDangerous] bit NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [catalog].[ProductCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_UnitsOfMeasure_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [configuration].[UnitsOfMeasure] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[Districts] (
        [Id] bigint NOT NULL IDENTITY,
        [ProvinceId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Districts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Districts_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[ShippingRateRules] (
        [Id] bigint NOT NULL IDENTITY,
        [ShippingRateCardId] bigint NOT NULL,
        [ServiceType] nvarchar(50) NOT NULL,
        [OriginProvinceId] bigint NULL,
        [DestinationProvinceId] bigint NULL,
        [FromWeightKg] decimal(12,3) NOT NULL,
        [ToWeightKg] decimal(12,3) NOT NULL,
        [BaseRate] decimal(19,4) NOT NULL,
        [AdditionalPerKgRate] decimal(19,4) NOT NULL,
        [SurchargePercent] decimal(5,2) NOT NULL,
        [MinimumFee] decimal(19,4) NOT NULL,
        [EstimatedDays] int NOT NULL,
        CONSTRAINT [PK_ShippingRateRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShippingRateRules_Provinces_DestinationProvinceId] FOREIGN KEY ([DestinationProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShippingRateRules_Provinces_OriginProvinceId] FOREIGN KEY ([OriginProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShippingRateRules_ShippingRateCards_ShippingRateCardId] FOREIGN KEY ([ShippingRateCardId]) REFERENCES [finance].[ShippingRateCards] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryLots] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [LotNumber] nvarchar(50) NOT NULL,
        [ManufactureDate] date NULL,
        [ExpiryDate] date NULL,
        [VendorBatchCode] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InventoryLots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryLots_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [configuration].[Wards] (
        [Id] bigint NOT NULL IDENTITY,
        [DistrictId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Wards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wards_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [configuration].[Districts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[Addresses] (
        [Id] bigint NOT NULL IDENTITY,
        [ContactName] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NULL,
        [AddressLine1] nvarchar(250) NOT NULL,
        [AddressLine2] nvarchar(250) NULL,
        [CountryId] bigint NOT NULL,
        [ProvinceId] bigint NULL,
        [DistrictId] bigint NULL,
        [WardId] bigint NULL,
        [PostalCode] nvarchar(20) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [FormattedAddress] nvarchar(500) NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [configuration].[Countries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Addresses_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [configuration].[Districts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Addresses_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Addresses_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [configuration].[Wards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [organization].[Branches] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [BranchType] nvarchar(50) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NULL,
        [AddressId] bigint NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Branches_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [crm].[Addresses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [organization].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [audit].[AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NULL,
        [Username] nvarchar(100) NULL,
        [UserRole] nvarchar(50) NULL,
        [Action] nvarchar(50) NOT NULL,
        [TableName] nvarchar(100) NOT NULL,
        [SchemaName] nvarchar(50) NOT NULL,
        [PrimaryKey] nvarchar(100) NOT NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [AffectedColumns] nvarchar(1000) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [Timestamp] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [carrier].[CarrierTrackingEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [CarrierId] bigint NOT NULL,
        [CarrierTrackingNumber] nvarchar(100) NOT NULL,
        [ExternalEventId] nvarchar(100) NOT NULL,
        [EventCode] nvarchar(100) NOT NULL,
        [EventDescription] nvarchar(500) NOT NULL,
        [EventTimestamp] datetime2(7) NOT NULL,
        [LocationName] nvarchar(200) NULL,
        [RawPayloadJson] nvarchar(max) NOT NULL,
        [ProcessedStatus] nvarchar(50) NOT NULL,
        [ProcessedShipmentId] bigint NULL,
        [ProcessedAt] datetime2(7) NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK_CarrierTrackingEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CarrierTrackingEvents_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[CODCollections] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [CollectedAmount] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [CollectionDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CollectedByEmployeeId] bigint NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [DriverReceiptNumber] nvarchar(50) NULL,
        [VerifiedByUserId] bigint NULL,
        [VerifiedAt] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CODCollections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CODCollections_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[CODRemittanceItems] (
        [Id] bigint NOT NULL IDENTITY,
        [CODRemittanceId] bigint NOT NULL,
        [CODCollectionId] bigint NOT NULL,
        [ShipmentId] bigint NOT NULL,
        [CodAmount] decimal(19,4) NOT NULL,
        [DeductedShippingFee] decimal(19,4) NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        CONSTRAINT [PK_CODRemittanceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CODRemittanceItems_CODCollections_CODCollectionId] FOREIGN KEY ([CODCollectionId]) REFERENCES [finance].[CODCollections] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[CODRemittances] (
        [Id] bigint NOT NULL IDENTITY,
        [RemittanceNumber] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [RemittanceDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [TotalCodAmount] decimal(19,4) NOT NULL,
        [TotalServiceFeesDeducted] decimal(19,4) NOT NULL,
        [NetRemittanceAmount] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [PaymentMethod] nvarchar(50) NOT NULL,
        [PaymentReference] nvarchar(100) NULL,
        [Status] nvarchar(50) NOT NULL,
        [BankName] nvarchar(100) NOT NULL,
        [BankAccount] nvarchar(50) NOT NULL,
        [AccountHolderName] nvarchar(150) NOT NULL,
        [RemittedByUserId] bigint NOT NULL,
        [RemittedAt] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CODRemittances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CODRemittances_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[CustomerAddresses] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerId] bigint NOT NULL,
        [AddressId] bigint NOT NULL,
        [AddressType] nvarchar(50) NOT NULL,
        [IsDefault] bit NOT NULL,
        [AddressLabel] nvarchar(100) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerAddresses_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [crm].[Addresses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[CustomerBalances] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerId] bigint NOT NULL,
        [CurrentOutstandingBalance] decimal(19,4) NOT NULL,
        [UnbilledChargesAmount] decimal(19,4) NOT NULL,
        [TotalCodHoldingAmount] decimal(19,4) NOT NULL,
        [LastUpdatedDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CustomerBalances] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[CustomerContacts] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerId] bigint NOT NULL,
        [ContactName] nvarchar(150) NOT NULL,
        [JobTitle] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NULL,
        [IsPrimary] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CustomerContacts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[CustomerContracts] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerId] bigint NOT NULL,
        [ContractNumber] nvarchar(50) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [DiscountRate] decimal(5,2) NOT NULL,
        [PaymentTermDays] int NOT NULL,
        [SignedAt] date NULL,
        [FileAttachmentId] bigint NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CustomerContracts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[CustomerPricingRules] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerId] bigint NOT NULL,
        [CustomerContractId] bigint NULL,
        [ShippingRateCardId] bigint NULL,
        [Name] nvarchar(150) NOT NULL,
        [DiscountPercent] decimal(5,2) NOT NULL,
        [FlatDiscountAmount] decimal(19,4) NOT NULL,
        [Priority] int NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_CustomerPricingRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerPricingRules_CustomerContracts_CustomerContractId] FOREIGN KEY ([CustomerContractId]) REFERENCES [crm].[CustomerContracts] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CustomerPricingRules_ShippingRateCards_ShippingRateCardId] FOREIGN KEY ([ShippingRateCardId]) REFERENCES [finance].[ShippingRateCards] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [crm].[Customers] (
        [Id] bigint NOT NULL IDENTITY,
        [BranchId] bigint NOT NULL,
        [CustomerCode] nvarchar(50) NOT NULL,
        [CustomerType] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TradeName] nvarchar(200) NULL,
        [TaxCode] nvarchar(50) NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [CreditLimit] decimal(19,4) NOT NULL,
        [CreditTermDays] int NOT NULL,
        [PaymentType] nvarchar(50) NOT NULL,
        [AccountManagerEmployeeId] bigint NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[FinancialLedgerEntries] (
        [Id] bigint NOT NULL IDENTITY,
        [EntryNumber] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [EntryType] nvarchar(50) NOT NULL,
        [ReferenceType] nvarchar(50) NOT NULL,
        [ReferenceId] bigint NOT NULL,
        [ReferenceNumber] nvarchar(50) NOT NULL,
        [DebitAmount] decimal(19,4) NOT NULL,
        [CreditAmount] decimal(19,4) NOT NULL,
        [RunningBalance] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [EntryDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Description] nvarchar(500) NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_FinancialLedgerEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FinancialLedgerEntries_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FinancialLedgerEntries_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[Invoices] (
        [Id] bigint NOT NULL IDENTITY,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [InvoiceDate] date NOT NULL,
        [DueDate] date NOT NULL,
        [PeriodStartDate] date NULL,
        [PeriodEndDate] date NULL,
        [SubTotal] decimal(19,4) NOT NULL,
        [TaxRate] decimal(5,2) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [PaidAmount] decimal(19,4) NOT NULL,
        [OutstandingAmount] AS [TotalAmount] - [PaidAmount],
        [Status] nvarchar(50) NOT NULL,
        [PaymentTermsDays] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [sales].[Orders] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderNumber] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [BranchId] bigint NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [RequestedPickupDate] datetime2 NULL,
        [RequiredDeliveryDate] datetime2 NULL,
        [OrderStatus] nvarchar(50) NOT NULL,
        [PaymentStatus] nvarchar(50) NOT NULL,
        [TotalWeightKg] decimal(12,3) NOT NULL,
        [TotalVolumeCbm] decimal(12,4) NOT NULL,
        [DeclaredValue] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [CodAmount] decimal(19,4) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CancelledAt] datetime2 NULL,
        [CancelledBy] nvarchar(100) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [sales].[OrderAddresses] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [AddressType] nvarchar(50) NOT NULL,
        [ContactName] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NULL,
        [AddressLine1] nvarchar(250) NOT NULL,
        [AddressLine2] nvarchar(250) NULL,
        [CountryId] bigint NOT NULL,
        [ProvinceId] bigint NULL,
        [DistrictId] bigint NULL,
        [WardId] bigint NULL,
        [PostalCode] nvarchar(20) NULL,
        [FormattedAddress] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_OrderAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderAddresses_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [configuration].[Countries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderAddresses_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [configuration].[Districts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderAddresses_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderAddresses_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderAddresses_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [configuration].[Wards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [sales].[OrderItems] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [ProductId] bigint NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [Sku] nvarchar(50) NULL,
        [Quantity] int NOT NULL,
        [UnitOfMeasureId] bigint NOT NULL,
        [UnitPrice] decimal(19,4) NOT NULL,
        [TotalPrice] decimal(19,4) NOT NULL,
        [DeclaredValue] decimal(19,4) NOT NULL,
        [WeightKg] decimal(12,3) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderItems_UnitsOfMeasure_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [configuration].[UnitsOfMeasure] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [sales].[OrderPackages] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [PackageCode] nvarchar(50) NOT NULL,
        [WeightKg] decimal(12,3) NOT NULL,
        [LengthCm] decimal(12,2) NOT NULL,
        [WidthCm] decimal(12,2) NOT NULL,
        [HeightCm] decimal(12,2) NOT NULL,
        [VolumetricWeightKg] decimal(12,3) NOT NULL,
        [DeclaredValue] decimal(19,4) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Barcode] nvarchar(100) NULL,
        [SpecialInstructions] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_OrderPackages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderPackages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[Shipments] (
        [Id] bigint NOT NULL IDENTITY,
        [TrackingNumber] nvarchar(50) NOT NULL,
        [OrderId] bigint NULL,
        [CustomerId] bigint NOT NULL,
        [OriginBranchId] bigint NOT NULL,
        [DestinationBranchId] bigint NOT NULL,
        [AssignedCarrierId] bigint NULL,
        [CarrierServiceId] bigint NULL,
        [CarrierTrackingNumber] nvarchar(100) NULL,
        [ShipmentStatus] nvarchar(50) NOT NULL,
        [ServiceType] nvarchar(50) NOT NULL,
        [TotalPackages] int NOT NULL,
        [ActualWeightKg] decimal(12,3) NOT NULL,
        [ChargeableWeightKg] decimal(12,3) NOT NULL,
        [DeclaredValue] decimal(19,4) NOT NULL,
        [CodAmount] decimal(19,4) NOT NULL,
        [ShippingFee] decimal(19,4) NOT NULL,
        [InsuranceFee] decimal(19,4) NOT NULL,
        [SurchargesTotal] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [PickupDate] datetime2 NULL,
        [EstimatedDeliveryDate] datetime2 NULL,
        [ActualDeliveryDate] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shipments_Branches_DestinationBranchId] FOREIGN KEY ([DestinationBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Branches_OriginBranchId] FOREIGN KEY ([OriginBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_CarrierServices_CarrierServiceId] FOREIGN KEY ([CarrierServiceId]) REFERENCES [carrier].[CarrierServices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Carriers_AssignedCarrierId] FOREIGN KEY ([AssignedCarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shipments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ShipmentAddresses] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [AddressType] nvarchar(50) NOT NULL,
        [ContactName] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NULL,
        [AddressLine1] nvarchar(250) NOT NULL,
        [AddressLine2] nvarchar(250) NULL,
        [CountryId] bigint NOT NULL,
        [ProvinceId] bigint NULL,
        [DistrictId] bigint NULL,
        [WardId] bigint NULL,
        [PostalCode] nvarchar(20) NULL,
        [FormattedAddress] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_ShipmentAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentAddresses_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [configuration].[Countries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAddresses_Districts_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [configuration].[Districts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAddresses_Provinces_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [configuration].[Provinces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAddresses_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ShipmentAddresses_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [configuration].[Wards] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[ShipmentCharges] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [ChargeType] nvarchar(50) NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsInvoiced] bit NOT NULL,
        [InvoiceId] bigint NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ShipmentCharges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentCharges_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentCharges_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [finance].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentCharges_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ShipmentPackages] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [OrderPackageId] bigint NULL,
        [PackageCode] nvarchar(50) NOT NULL,
        [Barcode] nvarchar(100) NOT NULL,
        [WeightKg] decimal(12,3) NOT NULL,
        [LengthCm] decimal(12,2) NOT NULL,
        [WidthCm] decimal(12,2) NOT NULL,
        [HeightCm] decimal(12,2) NOT NULL,
        [VolumetricWeightKg] decimal(12,3) NOT NULL,
        [PackageStatus] nvarchar(50) NOT NULL,
        [SealNumber] nvarchar(50) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ShipmentPackages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentPackages_OrderPackages_OrderPackageId] FOREIGN KEY ([OrderPackageId]) REFERENCES [sales].[OrderPackages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentPackages_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[InvoiceItems] (
        [Id] bigint NOT NULL IDENTITY,
        [InvoiceId] bigint NOT NULL,
        [ShipmentId] bigint NULL,
        [ShipmentChargeId] bigint NULL,
        [Description] nvarchar(250) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(19,4) NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [finance].[Invoices] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InvoiceItems_ShipmentCharges_ShipmentChargeId] FOREIGN KEY ([ShipmentChargeId]) REFERENCES [finance].[ShipmentCharges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItems_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[DeliveryAttempts] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [AttemptNumber] int NOT NULL,
        [AttemptedByEmployeeId] bigint NOT NULL,
        [AttemptedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Status] nvarchar(50) NOT NULL,
        [FailureReasonCode] nvarchar(50) NULL,
        [FailureNotes] nvarchar(500) NULL,
        [NextScheduledAttemptDate] date NULL,
        [RecipientContacted] bit NOT NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        CONSTRAINT [PK_DeliveryAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeliveryAttempts_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [organization].[Departments] (
        [Id] bigint NOT NULL IDENTITY,
        [CompanyId] bigint NOT NULL,
        [BranchId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ManagerEmployeeId] bigint NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Departments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [organization].[Companies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [organization].[Employees] (
        [Id] bigint NOT NULL IDENTITY,
        [BranchId] bigint NOT NULL,
        [DepartmentId] bigint NULL,
        [Code] nvarchar(50) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [FullName] nvarchar(200) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [DateOfBirth] date NULL,
        [Gender] nvarchar(20) NULL,
        [JobTitle] nvarchar(100) NOT NULL,
        [HireDate] date NOT NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT N'Active',
        [AddressId] bigint NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [crm].[Addresses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [organization].[Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[PickupRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [PickupCode] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [RequestedAddressId] bigint NOT NULL,
        [ScheduledDate] date NOT NULL,
        [TimeSlot] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [AssignedEmployeeId] bigint NULL,
        [ContactName] nvarchar(150) NOT NULL,
        [ContactPhone] nvarchar(50) NOT NULL,
        [EstimatedParcels] int NOT NULL,
        [ActualParcelsPickedUp] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PickupRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PickupRequests_Addresses_RequestedAddressId] FOREIGN KEY ([RequestedAddressId]) REFERENCES [crm].[Addresses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickupRequests_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PickupRequests_Employees_AssignedEmployeeId] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[UserAccounts] (
        [Id] bigint NOT NULL IDENTITY,
        [EmployeeId] bigint NULL,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [SecurityStamp] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsLockedOut] bit NOT NULL,
        [LockoutEndAt] datetime2 NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAccounts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [organization].[Warehouses] (
        [Id] bigint NOT NULL IDENTITY,
        [BranchId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [TotalAreaSqM] decimal(12,2) NOT NULL,
        [AddressId] bigint NULL,
        [ManagerEmployeeId] bigint NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouses_Addresses_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [crm].[Addresses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warehouses_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [audit].[FileAttachments] (
        [Id] bigint NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] bigint NULL,
        [FileName] nvarchar(250) NOT NULL,
        [StoredFileName] nvarchar(250) NOT NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [FileHashSha256] nvarchar(64) NOT NULL,
        [UploadedByUserId] bigint NULL,
        [UploadedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] bit NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_FileAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FileAttachments_UserAccounts_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[LoginHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NULL,
        [UsernameAttempted] nvarchar(50) NOT NULL,
        [IpAddress] nvarchar(50) NOT NULL,
        [UserAgent] nvarchar(500) NULL,
        [IsSuccessful] bit NOT NULL,
        [FailureReason] nvarchar(200) NULL,
        [AttemptedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginHistories_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [notification].[Notifications] (
        [Id] bigint NOT NULL IDENTITY,
        [TemplateId] bigint NULL,
        [RecipientUserId] bigint NULL,
        [RecipientEmail] nvarchar(150) NULL,
        [RecipientPhone] nvarchar(50) NULL,
        [Title] nvarchar(250) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Channel] nvarchar(50) NOT NULL,
        [Priority] nvarchar(20) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ReferenceType] nvarchar(50) NULL,
        [ReferenceId] bigint NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_NotificationTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [notification].[NotificationTemplates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notifications_UserAccounts_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[Payments] (
        [Id] bigint NOT NULL IDENTITY,
        [PaymentNumber] nvarchar(50) NOT NULL,
        [CustomerId] bigint NOT NULL,
        [PaymentDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [PaymentMethod] nvarchar(50) NOT NULL,
        [Amount] decimal(19,4) NOT NULL,
        [CurrencyId] bigint NOT NULL,
        [TransactionReference] nvarchar(100) NULL,
        [BankName] nvarchar(100) NULL,
        [BankAccount] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        [Status] nvarchar(50) NOT NULL,
        [ConfirmedByUserId] bigint NOT NULL,
        [ConfirmedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Currencies_CurrencyId] FOREIGN KEY ([CurrencyId]) REFERENCES [configuration].[Currencies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_UserAccounts_ConfirmedByUserId] FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[RefreshTokens] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [TokenHash] nvarchar(256) NOT NULL,
        [ExpiresAt] datetime2(7) NOT NULL,
        [IsRevoked] bit NOT NULL,
        [RevokedAt] datetime2(7) NULL,
        [ReplacedByToken] nvarchar(256) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedByIp] nvarchar(50) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ReturnRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [ReturnCode] nvarchar(50) NOT NULL,
        [OriginalShipmentId] bigint NOT NULL,
        [NewReturnShipmentId] bigint NULL,
        [ReasonCode] nvarchar(50) NOT NULL,
        [ReasonDescription] nvarchar(500) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [RequestedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ApprovedByUserId] bigint NULL,
        [ApprovedAt] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnRequests_Shipments_NewReturnShipmentId] FOREIGN KEY ([NewReturnShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReturnRequests_Shipments_OriginalShipmentId] FOREIGN KEY ([OriginalShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnRequests_UserAccounts_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ShipmentAssignments] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [AssignmentType] nvarchar(50) NOT NULL,
        [AssignedToEmployeeId] bigint NOT NULL,
        [AssignedBranchId] bigint NOT NULL,
        [AssignedVehicleId] nvarchar(50) NULL,
        [AssignedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AssignedByUserId] bigint NULL,
        [Status] nvarchar(50) NOT NULL,
        [CompletedAt] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_ShipmentAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentAssignments_Branches_AssignedBranchId] FOREIGN KEY ([AssignedBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAssignments_Employees_AssignedToEmployeeId] FOREIGN KEY ([AssignedToEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAssignments_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentAssignments_UserAccounts_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[UserBranchAccess] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [BranchId] bigint NOT NULL,
        [IsDefault] bit NOT NULL,
        [GrantedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [GrantedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_UserBranchAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserBranchAccess_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserBranchAccess_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[UserRoles] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [RoleId] bigint NOT NULL,
        [AssignedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AssignedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [identity].[UserSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [SessionTokenHash] nvarchar(256) NOT NULL,
        [DeviceInfo] nvarchar(250) NULL,
        [IpAddress] nvarchar(50) NOT NULL,
        [StartedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastActivityAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ExpiresAt] datetime2(7) NOT NULL,
        [IsTerminated] bit NOT NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryItems] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [ReorderPoint] int NOT NULL,
        [SafetyStock] int NOT NULL,
        [TargetStock] int NOT NULL,
        [MinimumOrderQuantity] int NOT NULL,
        [MaximumStock] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [TransactionNumber] nvarchar(50) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [TransactionType] nvarchar(50) NOT NULL,
        [ReferenceType] nvarchar(50) NOT NULL,
        [ReferenceId] bigint NOT NULL,
        [ReferenceNumber] nvarchar(50) NOT NULL,
        [PerformedByEmployeeId] bigint NOT NULL,
        [TransactionDate] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_Employees_PerformedByEmployeeId] FOREIGN KEY ([PerformedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[ParcelHandlingEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentPackageId] bigint NOT NULL,
        [ShipmentId] bigint NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [EventType] nvarchar(50) NOT NULL,
        [BagNumber] nvarchar(50) NULL,
        [NextDestinationWarehouseId] bigint NULL,
        [EmployeeId] bigint NOT NULL,
        [ScannedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [DeviceId] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ParcelHandlingEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ParcelHandlingEvents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ParcelHandlingEvents_ShipmentPackages_ShipmentPackageId] FOREIGN KEY ([ShipmentPackageId]) REFERENCES [shipping].[ShipmentPackages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ParcelHandlingEvents_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ParcelHandlingEvents_Warehouses_NextDestinationWarehouseId] FOREIGN KEY ([NextDestinationWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ParcelHandlingEvents_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ShipmentRoutes] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [SequenceOrder] int NOT NULL,
        [FromWarehouseId] bigint NOT NULL,
        [ToWarehouseId] bigint NOT NULL,
        [TransportType] nvarchar(50) NOT NULL,
        [CarrierId] bigint NULL,
        [VehicleNumber] nvarchar(50) NULL,
        [EstimatedDeparture] datetime2(7) NULL,
        [ActualDeparture] datetime2(7) NULL,
        [EstimatedArrival] datetime2(7) NULL,
        [ActualArrival] datetime2(7) NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_ShipmentRoutes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentRoutes_Carriers_CarrierId] FOREIGN KEY ([CarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ShipmentRoutes_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentRoutes_Warehouses_FromWarehouseId] FOREIGN KEY ([FromWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentRoutes_Warehouses_ToWarehouseId] FOREIGN KEY ([ToWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ShipmentStatusHistories] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [SubStatus] nvarchar(100) NULL,
        [LocationWarehouseId] bigint NULL,
        [LocationBranchId] bigint NULL,
        [Notes] nvarchar(1000) NULL,
        [ChangedByUserId] bigint NULL,
        [ChangedByEmployeeId] bigint NULL,
        [ChangedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Source] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ShipmentStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShipmentStatusHistories_Branches_LocationBranchId] FOREIGN KEY ([LocationBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentStatusHistories_Employees_ChangedByEmployeeId] FOREIGN KEY ([ChangedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ShipmentStatusHistories_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShipmentStatusHistories_UserAccounts_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ShipmentStatusHistories_Warehouses_LocationWarehouseId] FOREIGN KEY ([LocationWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[StockAdjustments] (
        [Id] bigint NOT NULL IDENTITY,
        [AdjustmentNumber] nvarchar(50) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [ReasonCode] nvarchar(50) NOT NULL,
        [ReasonDescription] nvarchar(250) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ApprovedByEmployeeId] bigint NULL,
        [ApprovedAt] datetime2(7) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StockAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockAdjustments_Employees_ApprovedByEmployeeId] FOREIGN KEY ([ApprovedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_StockAdjustments_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[StockTransfers] (
        [Id] bigint NOT NULL IDENTITY,
        [TransferNumber] nvarchar(50) NOT NULL,
        [SourceWarehouseId] bigint NOT NULL,
        [DestinationWarehouseId] bigint NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [RequestedByEmployeeId] bigint NOT NULL,
        [ApprovedByEmployeeId] bigint NULL,
        [ShippedAt] datetime2(7) NULL,
        [ReceivedAt] datetime2(7) NULL,
        [Notes] nvarchar(500) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StockTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockTransfers_Employees_ApprovedByEmployeeId] FOREIGN KEY ([ApprovedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_StockTransfers_Employees_RequestedByEmployeeId] FOREIGN KEY ([RequestedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Warehouses_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseDispatches] (
        [Id] bigint NOT NULL IDENTITY,
        [DispatchNumber] nvarchar(50) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [DispatchType] nvarchar(50) NOT NULL,
        [DestinationWarehouseId] bigint NULL,
        [DestinationBranchId] bigint NULL,
        [AssignedCarrierId] bigint NULL,
        [Status] nvarchar(50) NOT NULL,
        [DispatchedByEmployeeId] bigint NOT NULL,
        [DispatchedAt] datetime2(7) NULL,
        [VehicleNumber] nvarchar(50) NULL,
        [Notes] nvarchar(1000) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_WarehouseDispatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseDispatches_Branches_DestinationBranchId] FOREIGN KEY ([DestinationBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WarehouseDispatches_Carriers_AssignedCarrierId] FOREIGN KEY ([AssignedCarrierId]) REFERENCES [carrier].[Carriers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WarehouseDispatches_Employees_DispatchedByEmployeeId] FOREIGN KEY ([DispatchedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatches_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatches_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseReceipts] (
        [Id] bigint NOT NULL IDENTITY,
        [ReceiptNumber] nvarchar(50) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [ReceiptType] nvarchar(50) NOT NULL,
        [SourceBranchId] bigint NULL,
        [SourceVendor] nvarchar(200) NULL,
        [ExpectedArrivalDate] datetime2(7) NULL,
        [ActualArrivalDate] datetime2(7) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ReceivedByEmployeeId] bigint NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_WarehouseReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseReceipts_Branches_SourceBranchId] FOREIGN KEY ([SourceBranchId]) REFERENCES [organization].[Branches] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WarehouseReceipts_Employees_ReceivedByEmployeeId] FOREIGN KEY ([ReceivedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseReceipts_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseZones] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [ZoneType] nvarchar(50) NOT NULL,
        [TemperatureType] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_WarehouseZones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseZones_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [shipping].[ProofOfDeliveries] (
        [Id] bigint NOT NULL IDENTITY,
        [ShipmentId] bigint NOT NULL,
        [DeliveryAttemptId] bigint NOT NULL,
        [RecipientName] nvarchar(150) NOT NULL,
        [RecipientRelation] nvarchar(50) NOT NULL,
        [RecipientNationalId] nvarchar(50) NULL,
        [DeliveredAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [SignatureAttachmentId] bigint NULL,
        [PhotoAttachmentId] bigint NULL,
        [Notes] nvarchar(500) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [VerifiedByUserId] bigint NULL,
        [VerifiedAt] datetime2(7) NULL,
        CONSTRAINT [PK_ProofOfDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProofOfDeliveries_DeliveryAttempts_DeliveryAttemptId] FOREIGN KEY ([DeliveryAttemptId]) REFERENCES [shipping].[DeliveryAttempts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProofOfDeliveries_FileAttachments_PhotoAttachmentId] FOREIGN KEY ([PhotoAttachmentId]) REFERENCES [audit].[FileAttachments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProofOfDeliveries_FileAttachments_SignatureAttachmentId] FOREIGN KEY ([SignatureAttachmentId]) REFERENCES [audit].[FileAttachments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProofOfDeliveries_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProofOfDeliveries_UserAccounts_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [notification].[NotificationDeliveries] (
        [Id] bigint NOT NULL IDENTITY,
        [NotificationId] bigint NOT NULL,
        [Channel] nvarchar(50) NOT NULL,
        [AttemptNumber] int NOT NULL,
        [AttemptedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Status] nvarchar(50) NOT NULL,
        [GatewayResponse] nvarchar(1000) NULL,
        [ErrorDetails] nvarchar(1000) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_NotificationDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationDeliveries_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [notification].[Notifications] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [finance].[PaymentAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [PaymentId] bigint NOT NULL,
        [InvoiceId] bigint NOT NULL,
        [AllocatedAmount] decimal(19,4) NOT NULL,
        [AllocatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [AllocatedByUserId] bigint NULL,
        CONSTRAINT [PK_PaymentAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentAllocations_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [finance].[Invoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [finance].[Payments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentAllocations_UserAccounts_AllocatedByUserId] FOREIGN KEY ([AllocatedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseDispatchPackages] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseDispatchId] bigint NOT NULL,
        [ShipmentPackageId] bigint NOT NULL,
        [Barcode] nvarchar(100) NOT NULL,
        [ScannedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ScannedByEmployeeId] bigint NOT NULL,
        [TargetContainerCode] nvarchar(50) NULL,
        CONSTRAINT [PK_WarehouseDispatchPackages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseDispatchPackages_Employees_ScannedByEmployeeId] FOREIGN KEY ([ScannedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatchPackages_ShipmentPackages_ShipmentPackageId] FOREIGN KEY ([ShipmentPackageId]) REFERENCES [shipping].[ShipmentPackages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatchPackages_WarehouseDispatches_WarehouseDispatchId] FOREIGN KEY ([WarehouseDispatchId]) REFERENCES [warehouse].[WarehouseDispatches] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseLocations] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseZoneId] bigint NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Aisle] nvarchar(20) NULL,
        [Rack] nvarchar(20) NULL,
        [Shelf] nvarchar(20) NULL,
        [Bin] nvarchar(20) NULL,
        [Barcode] nvarchar(100) NOT NULL,
        [MaxWeightKg] decimal(12,3) NOT NULL,
        [MaxVolumeCbm] decimal(12,4) NOT NULL,
        [IsOccupied] bit NOT NULL,
        [IsBlocked] bit NOT NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_WarehouseLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseLocations_WarehouseZones_WarehouseZoneId] FOREIGN KEY ([WarehouseZoneId]) REFERENCES [warehouse].[WarehouseZones] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryBalances] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseId] bigint NOT NULL,
        [WarehouseLocationId] bigint NOT NULL,
        [ProductId] bigint NOT NULL,
        [LotId] bigint NULL,
        [QuantityOnHand] int NOT NULL,
        [QuantityAllocated] int NOT NULL,
        [QuantityAvailable] AS [QuantityOnHand] - [QuantityAllocated],
        [QuantityDamaged] int NOT NULL,
        [LastCountDate] datetime2(7) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InventoryBalances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryBalances_InventoryLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [inventory].[InventoryLots] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_InventoryBalances_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryBalances_WarehouseLocations_WarehouseLocationId] FOREIGN KEY ([WarehouseLocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryBalances_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryReservations] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [LocationId] bigint NOT NULL,
        [LotId] bigint NULL,
        [OrderId] bigint NOT NULL,
        [ReservedQuantity] int NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ReservedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ExpiresAt] datetime2(7) NOT NULL,
        [FulfilledAt] datetime2(7) NULL,
        [PublicId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InventoryReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryReservations_InventoryLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [inventory].[InventoryLots] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_InventoryReservations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryReservations_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryReservations_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryReservations_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventorySerialNumbers] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [SerialNumber] nvarchar(100) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [LocationId] bigint NULL,
        [Status] nvarchar(50) NOT NULL,
        [ReceivedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [DispatchedAt] datetime2(7) NULL,
        CONSTRAINT [PK_InventorySerialNumbers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventorySerialNumbers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventorySerialNumbers_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_InventorySerialNumbers_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [organization].[Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [inventory].[InventoryTransactionItems] (
        [Id] bigint NOT NULL IDENTITY,
        [InventoryTransactionId] bigint NOT NULL,
        [ProductId] bigint NOT NULL,
        [LocationId] bigint NOT NULL,
        [LotId] bigint NULL,
        [Quantity] int NOT NULL,
        [UnitCost] decimal(19,4) NOT NULL,
        [TotalCost] decimal(19,4) NOT NULL,
        [BalanceBefore] int NOT NULL,
        [BalanceAfter] int NOT NULL,
        CONSTRAINT [PK_InventoryTransactionItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactionItems_InventoryLots_LotId] FOREIGN KEY ([LotId]) REFERENCES [inventory].[InventoryLots] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_InventoryTransactionItems_InventoryTransactions_InventoryTransactionId] FOREIGN KEY ([InventoryTransactionId]) REFERENCES [inventory].[InventoryTransactions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryTransactionItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryTransactionItems_WarehouseLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseDispatchItems] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseDispatchId] bigint NOT NULL,
        [ProductId] bigint NOT NULL,
        [RequestedQuantity] int NOT NULL,
        [PickedQuantity] int NOT NULL,
        [UnitOfMeasureId] bigint NOT NULL,
        [SourceLocationId] bigint NOT NULL,
        [LotNumber] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_WarehouseDispatchItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseDispatchItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatchItems_UnitsOfMeasure_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [configuration].[UnitsOfMeasure] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseDispatchItems_WarehouseDispatches_WarehouseDispatchId] FOREIGN KEY ([WarehouseDispatchId]) REFERENCES [warehouse].[WarehouseDispatches] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WarehouseDispatchItems_WarehouseLocations_SourceLocationId] FOREIGN KEY ([SourceLocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE TABLE [warehouse].[WarehouseReceiptItems] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseReceiptId] bigint NOT NULL,
        [ProductId] bigint NOT NULL,
        [ExpectedQuantity] int NOT NULL,
        [ReceivedQuantity] int NOT NULL,
        [DamagedQuantity] int NOT NULL,
        [UnitOfMeasureId] bigint NOT NULL,
        [TargetLocationId] bigint NULL,
        [LotNumber] nvarchar(50) NULL,
        [ExpiryDate] date NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_WarehouseReceiptItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarehouseReceiptItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseReceiptItems_UnitsOfMeasure_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [configuration].[UnitsOfMeasure] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WarehouseReceiptItems_WarehouseLocations_TargetLocationId] FOREIGN KEY ([TargetLocationId]) REFERENCES [warehouse].[WarehouseLocations] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WarehouseReceiptItems_WarehouseReceipts_WarehouseReceiptId] FOREIGN KEY ([WarehouseReceiptId]) REFERENCES [warehouse].[WarehouseReceipts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Addresses_CountryId] ON [crm].[Addresses] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Addresses_DistrictId] ON [crm].[Addresses] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Addresses_ProvinceId] ON [crm].[Addresses] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Addresses_PublicId] ON [crm].[Addresses] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Addresses_WardId] ON [crm].[Addresses] ([WardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AuditLogs_PublicId] ON [audit].[AuditLogs] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [audit].[AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_BackgroundJobLogs_JobName] ON [integration].[BackgroundJobLogs] ([JobName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Branches_AddressId] ON [organization].[Branches] ([AddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_Code] ON [organization].[Branches] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Branches_CompanyId] ON [organization].[Branches] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_PublicId] ON [organization].[Branches] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierApiLogs_CarrierId] ON [carrier].[CarrierApiLogs] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierConfigurations_CarrierId] ON [carrier].[CarrierConfigurations] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carriers_Code] ON [carrier].[Carriers] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carriers_PublicId] ON [carrier].[Carriers] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierServices_CarrierId] ON [carrier].[CarrierServices] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CarrierServices_PublicId] ON [carrier].[CarrierServices] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierSyncJobs_CarrierId] ON [carrier].[CarrierSyncJobs] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierTrackingEvents_CarrierId] ON [carrier].[CarrierTrackingEvents] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierTrackingEvents_ProcessedShipmentId] ON [carrier].[CarrierTrackingEvents] ([ProcessedShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierTrackingMappings_CarrierId] ON [carrier].[CarrierTrackingMappings] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CarrierWebhookLogs_CarrierId] ON [carrier].[CarrierWebhookLogs] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODCollections_CollectedByEmployeeId] ON [finance].[CODCollections] ([CollectedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODCollections_CurrencyId] ON [finance].[CODCollections] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CODCollections_PublicId] ON [finance].[CODCollections] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODCollections_ShipmentId] ON [finance].[CODCollections] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODCollections_VerifiedByUserId] ON [finance].[CODCollections] ([VerifiedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CODRemittanceItems_CODCollectionId] ON [finance].[CODRemittanceItems] ([CODCollectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODRemittanceItems_CODRemittanceId] ON [finance].[CODRemittanceItems] ([CODRemittanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODRemittanceItems_ShipmentId] ON [finance].[CODRemittanceItems] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODRemittances_CurrencyId] ON [finance].[CODRemittances] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODRemittances_CustomerId] ON [finance].[CODRemittances] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CODRemittances_PublicId] ON [finance].[CODRemittances] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CODRemittances_RemittanceNumber] ON [finance].[CODRemittances] ([RemittanceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CODRemittances_RemittedByUserId] ON [finance].[CODRemittances] ([RemittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_Code] ON [organization].[Companies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_PublicId] ON [organization].[Companies] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_TaxNumber] ON [organization].[Companies] ([TaxNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Countries_Alpha3Code] ON [configuration].[Countries] ([Alpha3Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Countries_Code] ON [configuration].[Countries] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Currencies_Code] ON [configuration].[Currencies] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_AddressId] ON [crm].[CustomerAddresses] ([AddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [crm].[CustomerAddresses] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerAddresses_PublicId] ON [crm].[CustomerAddresses] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerBalances_CustomerId] ON [finance].[CustomerBalances] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerContacts_CustomerId] ON [crm].[CustomerContacts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerContacts_PublicId] ON [crm].[CustomerContacts] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerContracts_ContractNumber] ON [crm].[CustomerContracts] ([ContractNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerContracts_CustomerId] ON [crm].[CustomerContracts] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerContracts_FileAttachmentId] ON [crm].[CustomerContracts] ([FileAttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerContracts_PublicId] ON [crm].[CustomerContracts] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerPricingRules_CustomerContractId] ON [crm].[CustomerPricingRules] ([CustomerContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerPricingRules_CustomerId] ON [crm].[CustomerPricingRules] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerPricingRules_PublicId] ON [crm].[CustomerPricingRules] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_CustomerPricingRules_ShippingRateCardId] ON [crm].[CustomerPricingRules] ([ShippingRateCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Customers_AccountManagerEmployeeId] ON [crm].[Customers] ([AccountManagerEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Customers_BranchId] ON [crm].[Customers] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [crm].[Customers] ([CustomerCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_PublicId] ON [crm].[Customers] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_DeliveryAttempts_AttemptedByEmployeeId] ON [shipping].[DeliveryAttempts] ([AttemptedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_DeliveryAttempts_ShipmentId] ON [shipping].[DeliveryAttempts] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Departments_BranchId] ON [organization].[Departments] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Departments_CompanyId] ON [organization].[Departments] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Departments_ManagerEmployeeId] ON [organization].[Departments] ([ManagerEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_PublicId] ON [organization].[Departments] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Districts_Code] ON [configuration].[Districts] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Districts_ProvinceId] ON [configuration].[Districts] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Employees_AddressId] ON [organization].[Employees] ([AddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Employees_BranchId] ON [organization].[Employees] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Code] ON [organization].[Employees] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [organization].[Employees] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_PublicId] ON [organization].[Employees] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_FileAttachments_EntityType_EntityId] ON [audit].[FileAttachments] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FileAttachments_PublicId] ON [audit].[FileAttachments] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_FileAttachments_UploadedByUserId] ON [audit].[FileAttachments] ([UploadedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_FinancialLedgerEntries_CurrencyId] ON [finance].[FinancialLedgerEntries] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_FinancialLedgerEntries_CustomerId] ON [finance].[FinancialLedgerEntries] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FinancialLedgerEntries_EntryNumber] ON [finance].[FinancialLedgerEntries] ([EntryNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FinancialLedgerEntries_PublicId] ON [finance].[FinancialLedgerEntries] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryBalances_LotId] ON [inventory].[InventoryBalances] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryBalances_ProductId] ON [inventory].[InventoryBalances] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryBalances_WarehouseId_WarehouseLocationId_ProductId_LotId] ON [inventory].[InventoryBalances] ([WarehouseId], [WarehouseLocationId], [ProductId], [LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryBalances_WarehouseLocationId] ON [inventory].[InventoryBalances] ([WarehouseLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryItems_ProductId_WarehouseId] ON [inventory].[InventoryItems] ([ProductId], [WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryItems_PublicId] ON [inventory].[InventoryItems] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_WarehouseId] ON [inventory].[InventoryItems] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryLots_ProductId_LotNumber] ON [inventory].[InventoryLots] ([ProductId], [LotNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryLots_PublicId] ON [inventory].[InventoryLots] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryReservations_LocationId] ON [inventory].[InventoryReservations] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryReservations_LotId] ON [inventory].[InventoryReservations] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryReservations_OrderId] ON [inventory].[InventoryReservations] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryReservations_ProductId] ON [inventory].[InventoryReservations] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryReservations_PublicId] ON [inventory].[InventoryReservations] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryReservations_WarehouseId] ON [inventory].[InventoryReservations] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventorySerialNumbers_LocationId] ON [inventory].[InventorySerialNumbers] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventorySerialNumbers_ProductId_SerialNumber] ON [inventory].[InventorySerialNumbers] ([ProductId], [SerialNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventorySerialNumbers_WarehouseId] ON [inventory].[InventorySerialNumbers] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactionItems_InventoryTransactionId] ON [inventory].[InventoryTransactionItems] ([InventoryTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactionItems_LocationId] ON [inventory].[InventoryTransactionItems] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactionItems_LotId] ON [inventory].[InventoryTransactionItems] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactionItems_ProductId] ON [inventory].[InventoryTransactionItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_PerformedByEmployeeId] ON [inventory].[InventoryTransactions] ([PerformedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryTransactions_PublicId] ON [inventory].[InventoryTransactions] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryTransactions_TransactionNumber] ON [inventory].[InventoryTransactions] ([TransactionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [inventory].[InventoryTransactions] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [finance].[InvoiceItems] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_ShipmentChargeId] ON [finance].[InvoiceItems] ([ShipmentChargeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_ShipmentId] ON [finance].[InvoiceItems] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [finance].[Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [finance].[Invoices] ([InvoiceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_PublicId] ON [finance].[Invoices] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_LoginHistories_UserId] ON [identity].[LoginHistories] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_NotificationDeliveries_NotificationId] ON [notification].[NotificationDeliveries] ([NotificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_PublicId] ON [notification].[Notifications] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientUserId] ON [notification].[Notifications] ([RecipientUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Notifications_TemplateId] ON [notification].[Notifications] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NotificationTemplates_Code] ON [notification].[NotificationTemplates] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NotificationTemplates_PublicId] ON [notification].[NotificationTemplates] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NumberSequences_Scope] ON [configuration].[NumberSequences] ([Scope]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderAddresses_CountryId] ON [sales].[OrderAddresses] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderAddresses_DistrictId] ON [sales].[OrderAddresses] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderAddresses_OrderId] ON [sales].[OrderAddresses] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderAddresses_ProvinceId] ON [sales].[OrderAddresses] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderAddresses_WardId] ON [sales].[OrderAddresses] ([WardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [sales].[OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [sales].[OrderItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderItems_PublicId] ON [sales].[OrderItems] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderItems_UnitOfMeasureId] ON [sales].[OrderItems] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OrderPackages_OrderId] ON [sales].[OrderPackages] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderPackages_PublicId] ON [sales].[OrderPackages] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Orders_BranchId] ON [sales].[Orders] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Orders_CurrencyId] ON [sales].[Orders] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [sales].[Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [sales].[Orders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_PublicId] ON [sales].[Orders] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_EventType] ON [integration].[OutboxMessages] ([EventType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_Status] ON [integration].[OutboxMessages] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ParcelHandlingEvents_EmployeeId] ON [warehouse].[ParcelHandlingEvents] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ParcelHandlingEvents_NextDestinationWarehouseId] ON [warehouse].[ParcelHandlingEvents] ([NextDestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ParcelHandlingEvents_ShipmentId] ON [warehouse].[ParcelHandlingEvents] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ParcelHandlingEvents_ShipmentPackageId] ON [warehouse].[ParcelHandlingEvents] ([ShipmentPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ParcelHandlingEvents_WarehouseId] ON [warehouse].[ParcelHandlingEvents] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_AllocatedByUserId] ON [finance].[PaymentAllocations] ([AllocatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_InvoiceId] ON [finance].[PaymentAllocations] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PaymentAllocations_PaymentId] ON [finance].[PaymentAllocations] ([PaymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Payments_ConfirmedByUserId] ON [finance].[Payments] ([ConfirmedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Payments_CurrencyId] ON [finance].[Payments] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Payments_CustomerId] ON [finance].[Payments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_PaymentNumber] ON [finance].[Payments] ([PaymentNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_PublicId] ON [finance].[Payments] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Code] ON [identity].[Permissions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_PublicId] ON [identity].[Permissions] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PickupRequests_AssignedEmployeeId] ON [shipping].[PickupRequests] ([AssignedEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PickupRequests_CustomerId] ON [shipping].[PickupRequests] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PickupRequests_PickupCode] ON [shipping].[PickupRequests] ([PickupCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PickupRequests_PublicId] ON [shipping].[PickupRequests] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_PickupRequests_RequestedAddressId] ON [shipping].[PickupRequests] ([RequestedAddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductCategories_Code] ON [catalog].[ProductCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ProductCategories_ParentCategoryId] ON [catalog].[ProductCategories] ([ParentCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductCategories_PublicId] ON [catalog].[ProductCategories] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Products_Barcode] ON [catalog].[Products] ([Barcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [catalog].[Products] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Products_CurrencyId] ON [catalog].[Products] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_PublicId] ON [catalog].[Products] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [catalog].[Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Products_UnitOfMeasureId] ON [catalog].[Products] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProofOfDeliveries_DeliveryAttemptId] ON [shipping].[ProofOfDeliveries] ([DeliveryAttemptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ProofOfDeliveries_PhotoAttachmentId] ON [shipping].[ProofOfDeliveries] ([PhotoAttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProofOfDeliveries_ShipmentId] ON [shipping].[ProofOfDeliveries] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ProofOfDeliveries_SignatureAttachmentId] ON [shipping].[ProofOfDeliveries] ([SignatureAttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ProofOfDeliveries_VerifiedByUserId] ON [shipping].[ProofOfDeliveries] ([VerifiedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Provinces_Code] ON [configuration].[Provinces] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Provinces_CountryId] ON [configuration].[Provinces] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_TokenHash] ON [identity].[RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [identity].[RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportDefinitions_Code] ON [notification].[ReportDefinitions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportDefinitions_PublicId] ON [notification].[ReportDefinitions] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ReturnRequests_ApprovedByUserId] ON [shipping].[ReturnRequests] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ReturnRequests_NewReturnShipmentId] ON [shipping].[ReturnRequests] ([NewReturnShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ReturnRequests_OriginalShipmentId] ON [shipping].[ReturnRequests] ([OriginalShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReturnRequests_PublicId] ON [shipping].[ReturnRequests] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReturnRequests_ReturnCode] ON [shipping].[ReturnRequests] ([ReturnCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [identity].[RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [identity].[RolePermissions] ([RoleId], [PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Code] ON [identity].[Roles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_PublicId] ON [identity].[Roles] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ScheduledReports_PublicId] ON [notification].[ScheduledReports] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ScheduledReports_ReportDefinitionId] ON [notification].[ScheduledReports] ([ReportDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAddresses_CountryId] ON [shipping].[ShipmentAddresses] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAddresses_DistrictId] ON [shipping].[ShipmentAddresses] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAddresses_ProvinceId] ON [shipping].[ShipmentAddresses] ([ProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAddresses_ShipmentId] ON [shipping].[ShipmentAddresses] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAddresses_WardId] ON [shipping].[ShipmentAddresses] ([WardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAssignments_AssignedBranchId] ON [shipping].[ShipmentAssignments] ([AssignedBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAssignments_AssignedByUserId] ON [shipping].[ShipmentAssignments] ([AssignedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAssignments_AssignedToEmployeeId] ON [shipping].[ShipmentAssignments] ([AssignedToEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentAssignments_ShipmentId] ON [shipping].[ShipmentAssignments] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentCharges_CurrencyId] ON [finance].[ShipmentCharges] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentCharges_InvoiceId] ON [finance].[ShipmentCharges] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentCharges_ShipmentId] ON [finance].[ShipmentCharges] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentPackages_Barcode] ON [shipping].[ShipmentPackages] ([Barcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentPackages_OrderPackageId] ON [shipping].[ShipmentPackages] ([OrderPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShipmentPackages_PublicId] ON [shipping].[ShipmentPackages] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentPackages_ShipmentId] ON [shipping].[ShipmentPackages] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentRoutes_CarrierId] ON [shipping].[ShipmentRoutes] ([CarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentRoutes_FromWarehouseId] ON [shipping].[ShipmentRoutes] ([FromWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentRoutes_ShipmentId] ON [shipping].[ShipmentRoutes] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentRoutes_ToWarehouseId] ON [shipping].[ShipmentRoutes] ([ToWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_AssignedCarrierId] ON [shipping].[Shipments] ([AssignedCarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_CarrierServiceId] ON [shipping].[Shipments] ([CarrierServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_CarrierTrackingNumber] ON [shipping].[Shipments] ([CarrierTrackingNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_CurrencyId] ON [shipping].[Shipments] ([CurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_CustomerId] ON [shipping].[Shipments] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_DestinationBranchId] ON [shipping].[Shipments] ([DestinationBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_OrderId] ON [shipping].[Shipments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Shipments_OriginBranchId] ON [shipping].[Shipments] ([OriginBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_PublicId] ON [shipping].[Shipments] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shipments_TrackingNumber] ON [shipping].[Shipments] ([TrackingNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentStatusHistories_ChangedByEmployeeId] ON [shipping].[ShipmentStatusHistories] ([ChangedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentStatusHistories_ChangedByUserId] ON [shipping].[ShipmentStatusHistories] ([ChangedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentStatusHistories_LocationBranchId] ON [shipping].[ShipmentStatusHistories] ([LocationBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentStatusHistories_LocationWarehouseId] ON [shipping].[ShipmentStatusHistories] ([LocationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShipmentStatusHistories_ShipmentId] ON [shipping].[ShipmentStatusHistories] ([ShipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShippingRateCards_BaseCurrencyId] ON [finance].[ShippingRateCards] ([BaseCurrencyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingRateCards_Code] ON [finance].[ShippingRateCards] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingRateCards_PublicId] ON [finance].[ShippingRateCards] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShippingRateRules_DestinationProvinceId] ON [finance].[ShippingRateRules] ([DestinationProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShippingRateRules_OriginProvinceId] ON [finance].[ShippingRateRules] ([OriginProvinceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_ShippingRateRules_ShippingRateCardId] ON [finance].[ShippingRateRules] ([ShippingRateCardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockAdjustments_AdjustmentNumber] ON [inventory].[StockAdjustments] ([AdjustmentNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_ApprovedByEmployeeId] ON [inventory].[StockAdjustments] ([ApprovedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockAdjustments_PublicId] ON [inventory].[StockAdjustments] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockAdjustments_WarehouseId] ON [inventory].[StockAdjustments] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_ApprovedByEmployeeId] ON [inventory].[StockTransfers] ([ApprovedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_DestinationWarehouseId] ON [inventory].[StockTransfers] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransfers_PublicId] ON [inventory].[StockTransfers] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_RequestedByEmployeeId] ON [inventory].[StockTransfers] ([RequestedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_SourceWarehouseId] ON [inventory].[StockTransfers] ([SourceWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransfers_TransferNumber] ON [inventory].[StockTransfers] ([TransferNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [configuration].[SystemSettings] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnitsOfMeasure_Code] ON [configuration].[UnitsOfMeasure] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_Email] ON [identity].[UserAccounts] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserAccounts_EmployeeId] ON [identity].[UserAccounts] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_PublicId] ON [identity].[UserAccounts] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_Username] ON [identity].[UserAccounts] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_UserBranchAccess_BranchId] ON [identity].[UserBranchAccess] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserBranchAccess_UserId_BranchId] ON [identity].[UserBranchAccess] ([UserId], [BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [identity].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [identity].[UserRoles] ([UserId], [RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_UserSessions_SessionTokenHash] ON [identity].[UserSessions] ([SessionTokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId] ON [identity].[UserSessions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wards_Code] ON [configuration].[Wards] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Wards_DistrictId] ON [configuration].[Wards] ([DistrictId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatches_AssignedCarrierId] ON [warehouse].[WarehouseDispatches] ([AssignedCarrierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatches_DestinationBranchId] ON [warehouse].[WarehouseDispatches] ([DestinationBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatches_DestinationWarehouseId] ON [warehouse].[WarehouseDispatches] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatches_DispatchedByEmployeeId] ON [warehouse].[WarehouseDispatches] ([DispatchedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseDispatches_DispatchNumber] ON [warehouse].[WarehouseDispatches] ([DispatchNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseDispatches_PublicId] ON [warehouse].[WarehouseDispatches] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatches_WarehouseId] ON [warehouse].[WarehouseDispatches] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchItems_ProductId] ON [warehouse].[WarehouseDispatchItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchItems_SourceLocationId] ON [warehouse].[WarehouseDispatchItems] ([SourceLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchItems_UnitOfMeasureId] ON [warehouse].[WarehouseDispatchItems] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchItems_WarehouseDispatchId] ON [warehouse].[WarehouseDispatchItems] ([WarehouseDispatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchPackages_ScannedByEmployeeId] ON [warehouse].[WarehouseDispatchPackages] ([ScannedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchPackages_ShipmentPackageId] ON [warehouse].[WarehouseDispatchPackages] ([ShipmentPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseDispatchPackages_WarehouseDispatchId] ON [warehouse].[WarehouseDispatchPackages] ([WarehouseDispatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseLocations_Barcode] ON [warehouse].[WarehouseLocations] ([Barcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseLocations_PublicId] ON [warehouse].[WarehouseLocations] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseLocations_WarehouseZoneId] ON [warehouse].[WarehouseLocations] ([WarehouseZoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceiptItems_ProductId] ON [warehouse].[WarehouseReceiptItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceiptItems_TargetLocationId] ON [warehouse].[WarehouseReceiptItems] ([TargetLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceiptItems_UnitOfMeasureId] ON [warehouse].[WarehouseReceiptItems] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceiptItems_WarehouseReceiptId] ON [warehouse].[WarehouseReceiptItems] ([WarehouseReceiptId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseReceipts_PublicId] ON [warehouse].[WarehouseReceipts] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseReceipts_ReceiptNumber] ON [warehouse].[WarehouseReceipts] ([ReceiptNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_ReceivedByEmployeeId] ON [warehouse].[WarehouseReceipts] ([ReceivedByEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_SourceBranchId] ON [warehouse].[WarehouseReceipts] ([SourceBranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseReceipts_WarehouseId] ON [warehouse].[WarehouseReceipts] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Warehouses_AddressId] ON [organization].[Warehouses] ([AddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Warehouses_BranchId] ON [organization].[Warehouses] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [organization].[Warehouses] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_Warehouses_ManagerEmployeeId] ON [organization].[Warehouses] ([ManagerEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Warehouses_PublicId] ON [organization].[Warehouses] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WarehouseZones_PublicId] ON [warehouse].[WarehouseZones] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    CREATE INDEX [IX_WarehouseZones_WarehouseId] ON [warehouse].[WarehouseZones] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [audit].[AuditLogs] ADD CONSTRAINT [FK_AuditLogs_UserAccounts_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [carrier].[CarrierTrackingEvents] ADD CONSTRAINT [FK_CarrierTrackingEvents_Shipments_ProcessedShipmentId] FOREIGN KEY ([ProcessedShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODCollections] ADD CONSTRAINT [FK_CODCollections_Employees_CollectedByEmployeeId] FOREIGN KEY ([CollectedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODCollections] ADD CONSTRAINT [FK_CODCollections_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODCollections] ADD CONSTRAINT [FK_CODCollections_UserAccounts_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODRemittanceItems] ADD CONSTRAINT [FK_CODRemittanceItems_CODRemittances_CODRemittanceId] FOREIGN KEY ([CODRemittanceId]) REFERENCES [finance].[CODRemittances] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODRemittanceItems] ADD CONSTRAINT [FK_CODRemittanceItems_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [shipping].[Shipments] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODRemittances] ADD CONSTRAINT [FK_CODRemittances_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CODRemittances] ADD CONSTRAINT [FK_CODRemittances_UserAccounts_RemittedByUserId] FOREIGN KEY ([RemittedByUserId]) REFERENCES [identity].[UserAccounts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[CustomerAddresses] ADD CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [finance].[CustomerBalances] ADD CONSTRAINT [FK_CustomerBalances_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[CustomerContacts] ADD CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[CustomerContracts] ADD CONSTRAINT [FK_CustomerContracts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[CustomerContracts] ADD CONSTRAINT [FK_CustomerContracts_FileAttachments_FileAttachmentId] FOREIGN KEY ([FileAttachmentId]) REFERENCES [audit].[FileAttachments] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[CustomerPricingRules] ADD CONSTRAINT [FK_CustomerPricingRules_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [crm].[Customers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [crm].[Customers] ADD CONSTRAINT [FK_Customers_Employees_AccountManagerEmployeeId] FOREIGN KEY ([AccountManagerEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [shipping].[DeliveryAttempts] ADD CONSTRAINT [FK_DeliveryAttempts_Employees_AttemptedByEmployeeId] FOREIGN KEY ([AttemptedByEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    ALTER TABLE [organization].[Departments] ADD CONSTRAINT [FK_Departments_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [organization].[Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118151401_InitialDatabaseSetup'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118151401_InitialDatabaseSetup', N'9.0.4');
END;

COMMIT;
GO

