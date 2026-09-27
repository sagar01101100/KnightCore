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
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Audit] (
        [Id] uniqueidentifier NOT NULL,
        [ActorId] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Audit] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [IdempotencyRecords] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [Key] nvarchar(128) NOT NULL,
        [RequestHash] nvarchar(max) NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_IdempotencyRecords] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [InvoiceSeries] (
        [Year] int NOT NULL IDENTITY,
        [NextNumber] bigint NOT NULL,
        [ConcurrencyToken] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InvoiceSeries] PRIMARY KEY ([Year])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Jobs] (
        [Id] uniqueidentifier NOT NULL,
        [Kind] nvarchar(max) NOT NULL,
        [DeduplicationKey] nvarchar(200) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [State] nvarchar(450) NOT NULL,
        [Attempts] int NOT NULL,
        [AvailableUtc] datetime2 NOT NULL,
        [LeaseUntilUtc] datetime2 NULL,
        [LeaseToken] uniqueidentifier NULL,
        [LastError] nvarchar(max) NULL,
        CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Segments] (
        [Id] nvarchar(80) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Icon] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Segments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [BillingProfiles] (
        [CustomerId] nvarchar(450) NOT NULL,
        [DetailsJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_BillingProfiles] PRIMARY KEY ([CustomerId]),
        CONSTRAINT [FK_BillingProfiles_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Configurations] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [SelectionJson] nvarchar(max) NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Configurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Configurations_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [CustomRequests] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [PackageName] nvarchar(max) NOT NULL,
        [SelectionJson] nvarchar(max) NOT NULL,
        [PriceSnapshotJson] nvarchar(max) NOT NULL,
        [BillingJson] nvarchar(max) NOT NULL,
        [Requirements] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [QuoteId] uniqueidentifier NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomRequests_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Quotes] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [Version] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ConcurrencyToken] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quotes_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Packages] (
        [Id] nvarchar(100) NOT NULL,
        [SegmentId] nvarchar(80) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Summary] nvarchar(max) NOT NULL,
        [Accent] nvarchar(max) NOT NULL,
        [Published] bit NOT NULL,
        CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Packages_Segments_SegmentId] FOREIGN KEY ([SegmentId]) REFERENCES [Segments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [QuoteId] uniqueidentifier NOT NULL,
        [QuoteVersion] int NOT NULL,
        [Number] nvarchar(450) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [DeliveryUrl] nvarchar(max) NULL,
        [ConcurrencyToken] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [QuoteRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [QuoteId] uniqueidentifier NOT NULL,
        [Version] int NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_QuoteRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuoteRevisions_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [PackageVersions] (
        [Id] nvarchar(450) NOT NULL,
        [PackageId] nvarchar(100) NOT NULL,
        [Version] int NOT NULL,
        [BasePrice] decimal(19,2) NOT NULL,
        [IncludedJson] nvarchar(max) NOT NULL,
        [Delivery] nvarchar(max) NOT NULL,
        [Current] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_PackageVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageVersions_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Number] nvarchar(450) NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [PdfStatus] nvarchar(max) NOT NULL,
        [ObjectKey] nvarchar(max) NULL,
        [Sha256] nvarchar(max) NULL,
        [IssuedUtc] datetime2 NOT NULL,
        [DueUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [OrderHistory] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [ActorId] nvarchar(max) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderHistory_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE TABLE [Features] (
        [Id] nvarchar(450) NOT NULL,
        [PackageVersionId] nvarchar(450) NOT NULL,
        [Code] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [PriceMode] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(19,2) NOT NULL,
        [MaxQuantity] int NOT NULL,
        [RequiresJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Features] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Features_PackageVersions_PackageVersionId] FOREIGN KEY ([PackageVersionId]) REFERENCES [PackageVersions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Configurations_CustomerId] ON [Configurations] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_CustomRequests_CustomerId_CreatedUtc] ON [CustomRequests] ([CustomerId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Features_PackageVersionId_Code] ON [Features] ([PackageVersionId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IdempotencyRecords_CustomerId_Key] ON [IdempotencyRecords] ([CustomerId], [Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_Number] ON [Invoices] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_OrderId] ON [Invoices] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Jobs_DeduplicationKey] ON [Jobs] ([DeduplicationKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Jobs_State_AvailableUtc] ON [Jobs] ([State], [AvailableUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_OrderHistory_OrderId] ON [OrderHistory] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId_CreatedUtc] ON [Orders] ([CustomerId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_Number] ON [Orders] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_QuoteId] ON [Orders] ([QuoteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Packages_SegmentId] ON [Packages] ([SegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PackageVersions_PackageId_Version] ON [PackageVersions] ([PackageId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuoteRevisions_QuoteId_Version] ON [QuoteRevisions] ([QuoteId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Quotes_CustomerId_CreatedUtc] ON [Quotes] ([CustomerId], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927044910_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260927044910_InitialSchema', N'10.0.12');
END;

COMMIT;
GO

