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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [NamaLengkap] nvarchar(100) NOT NULL,
        [NoTelepon] nvarchar(20) NOT NULL,
        [Alamat] nvarchar(500) NULL,
        [TanggalDaftar] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE TABLE [PermitApplications] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationNumber] nvarchar(50) NOT NULL,
        [UserId] int NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [CompanyAddress] nvarchar(500) NOT NULL,
        [OriginLocation] nvarchar(100) NOT NULL,
        [DestinationLocation] nvarchar(100) NOT NULL,
        [DeparturePort] nvarchar(100) NOT NULL,
        [ArrivalPort] nvarchar(100) NOT NULL,
        [Status] int NOT NULL,
        [CurrentApprovalLevel] int NOT NULL,
        [SubmissionDate] datetime2 NOT NULL,
        [ValidFrom] datetime2 NULL,
        [ValidUntil] datetime2 NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [AdminId] int NULL,
        [AdminApprovalDate] datetime2 NULL,
        [VerifikatorId] int NULL,
        [VerificationDate] datetime2 NULL,
        [KepalaDinasId] int NULL,
        [FinalApprovalDate] datetime2 NULL,
        [GeneratedDocumentPath] nvarchar(500) NULL,
        CONSTRAINT [PK_PermitApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermitApplications_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermitApplications_Users_KepalaDinasId] FOREIGN KEY ([KepalaDinasId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermitApplications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_PermitApplications_Users_VerifikatorId] FOREIGN KEY ([VerifikatorId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE TABLE [LivestockDetails] (
        [Id] int NOT NULL IDENTITY,
        [PermitApplicationId] int NOT NULL,
        [LivestockType] nvarchar(50) NOT NULL,
        [Quantity] int NOT NULL,
        [Description] nvarchar(200) NULL,
        CONSTRAINT [PK_LivestockDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LivestockDetails_PermitApplications_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [PermitApplications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE TABLE [PermitApprovalHistories] (
        [Id] int NOT NULL IDENTITY,
        [PermitApplicationId] int NOT NULL,
        [UserId] int NOT NULL,
        [FromStatus] int NOT NULL,
        [ToStatus] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Comments] nvarchar(1000) NULL,
        [ActionDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PermitApprovalHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermitApprovalHistories_PermitApplications_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [PermitApplications] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PermitApprovalHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE TABLE [PermitDocuments] (
        [Id] int NOT NULL IDENTITY,
        [PermitApplicationId] int NOT NULL,
        [DocumentName] nvarchar(200) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [FileSize] bigint NOT NULL,
        [FileExtension] nvarchar(10) NOT NULL,
        [UploadDate] datetime2 NOT NULL,
        [UploadedByUserId] int NOT NULL,
        CONSTRAINT [PK_PermitDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PermitDocuments_PermitApplications_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [PermitApplications] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PermitDocuments_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory]
               WHERE [MigrationId] = N'20250616051549_NewApprovalFlow')
BEGIN
    -- ON-kan IDENTITY biar bisa set Id manual (1..8)
    SET IDENTITY_INSERT [Users] ON;

    INSERT INTO [Users] ([Id], [Alamat], [Email], [IsActive], [NamaLengkap], [NoTelepon], [Password], [Role], [TanggalDaftar], [Username])
    VALUES 
    (1, 'Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram', 'kepaladinas@dpmptsp-ntb.go.id', 1, 'Hj. Eva Dewiyani, SP', '081234567890', '$2a$11$BCGyEESCpVMrVerzreURR.3BZ9uitmtMiaoKJU2pslyMH65gh.xZ.', 'KepalaDinas', '2024-01-01T00:00:00', 'kepaladinas'),
    (2, 'Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram', 'admin1@dpmptsp-ntb.go.id', 1, 'Ahmad Admin, S.Pt', '081234567891', '$2a$11$roTC0yhCxb.55PJ/PSMz/uFYEQw00YB657Co8cj8d2M0BzqYFexse', 'Admin', '2024-01-15T00:00:00', 'admin1'),
    (3, 'Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram', 'admin2@dpmptsp-ntb.go.id', 1, 'Siti Admin, S.Pt', '081234567893', '$2a$11$dlnS3JhriSvJcFWWOwX9GefAc2Lk3YbsrFk5VLOjYfHEV0oQOBF9m', 'Admin', '2024-01-20T00:00:00', 'admin2'),
    (4, 'Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram', 'verifikator1@dpmptsp-ntb.go.id', 1, 'Budi Verifikator, S.Pt, M.Si', '081234567894', '$2a$11$Di196bGJ3yOV.whx3TkB.eCnmtV03M/9DIWDIuEzGZFCCU0HK9qZq', 'Verifikator', '2024-01-25T00:00:00', 'verifikator1'),
    (5, 'Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram', 'verifikator2@dpmptsp-ntb.go.id', 1, 'Rina Verifikator, S.Pt', '081234567895', '$2a$11$ShNRaNjMF/IxhcOoZGrUuOxI6Pr41tJujx6EuPP5iy.gtenwe.bwi', 'Verifikator', '2024-01-30T00:00:00', 'verifikator2'),
    (6, 'Desa Suka Maju, Kec. Praya, Lombok Tengah', 'user1@example.com', 1, 'Budi Peternak', '081234567896', '$2a$11$n6zBiQ8IgPdOHkghfqQGru2DMtzkqxjOSzUM53teW1UAO73CpmOQO', 'User', '2024-02-01T00:00:00', 'user1'),
    (7, 'Desa Dena, Kec. Madapangga, Kab. Bima', 'cvdena@example.com', 1, 'CV. DENA BERSAUDARA', '081234567897', '$2a$11$Xe5nSUyr88T3rG.Qe/OtD.hzXqtKFW5TQ3U9WYBcCxaOD6x/b3f5a', 'User', '2024-03-01T00:00:00', 'cvdena'),
    (8, 'Jl. Peternakan No. 15, Mataram', 'sarimakmur@example.com', 1, 'PT. Sari Makmur Ternak', '081234567898', '$2a$11$4UyeoBPsfBrqzMEbm25mMuqTLWWigAusH4PuyoSPwi.YRvZYbqCzG', 'User', '2024-03-15T00:00:00', 'sarimakmur');

    SET IDENTITY_INSERT [Users] OFF;
END;
GO


IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_LivestockDetails_PermitApplicationId] ON [LivestockDetails] ([PermitApplicationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_AdminId] ON [PermitApplications] ([AdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PermitApplications_ApplicationNumber] ON [PermitApplications] ([ApplicationNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_KepalaDinasId] ON [PermitApplications] ([KepalaDinasId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_Status] ON [PermitApplications] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_SubmissionDate] ON [PermitApplications] ([SubmissionDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_UserId] ON [PermitApplications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_VerifikatorId] ON [PermitApplications] ([VerifikatorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApprovalHistories_ActionDate] ON [PermitApprovalHistories] ([ActionDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApprovalHistories_PermitApplicationId] ON [PermitApprovalHistories] ([PermitApplicationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitApprovalHistories_UserId] ON [PermitApprovalHistories] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitDocuments_PermitApplicationId] ON [PermitDocuments] ([PermitApplicationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE INDEX [IX_PermitDocuments_UploadedByUserId] ON [PermitDocuments] ([UploadedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250616051549_NewApprovalFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250616051549_NewApprovalFlow', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_AdminId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_KepalaDinasId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_VerifikatorId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 7;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    EXEC(N'DELETE FROM [Users]
    WHERE [Id] = 8;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [Users] ADD [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetToken] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [Users] ADD [ResetTokenExpires] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [Users] ADD [VerificationToken] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [Users] ADD [VerificationTokenExpires] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitDocuments] ADD [DocumentDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitDocuments] ADD [DocumentDescription] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitDocuments] ADD [DocumentNumber] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PermitApplications]') AND [c].[name] = N'CompanyName');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [PermitApplications] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [PermitApplications] ALTER COLUMN [CompanyName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PermitApplications]') AND [c].[name] = N'CompanyAddress');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [PermitApplications] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [PermitApplications] ALTER COLUMN [CompanyAddress] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD [ApplicantType] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD [DestinationProvinceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD [DestinationRegencyId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD [OriginProvinceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD [OriginRegencyId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE TABLE [LivestockQuotas] (
        [Id] int NOT NULL IDENTITY,
        [LivestockType] nvarchar(50) NOT NULL,
        [ProvinceCode] nvarchar(5) NOT NULL,
        [ProvinceName] nvarchar(50) NOT NULL,
        [Year] int NOT NULL,
        [TotalQuota] int NOT NULL,
        [UsedQuota] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [RegulationReference] nvarchar(100) NULL,
        CONSTRAINT [PK_LivestockQuotas] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE TABLE [Ports] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [Province] nvarchar(50) NOT NULL,
        [ProvinceCode] nvarchar(5) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Ports] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE TABLE [QuotaUsages] (
        [Id] int NOT NULL IDENTITY,
        [LivestockQuotaId] int NOT NULL,
        [PermitApplicationId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UsedAt] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Notes] nvarchar(200) NULL,
        CONSTRAINT [PK_QuotaUsages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuotaUsages_LivestockQuotas_LivestockQuotaId] FOREIGN KEY ([LivestockQuotaId]) REFERENCES [LivestockQuotas] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuotaUsages_PermitApplications_PermitApplicationId] FOREIGN KEY ([PermitApplicationId]) REFERENCES [PermitApplications] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitDocuments_DocumentDate] ON [PermitDocuments] ([DocumentDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitDocuments_DocumentNumber] ON [PermitDocuments] ([DocumentNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitDocuments_DocumentType] ON [PermitDocuments] ([DocumentType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_DestinationProvinceId] ON [PermitApplications] ([DestinationProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_DestinationRegencyId] ON [PermitApplications] ([DestinationRegencyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_OriginProvinceId] ON [PermitApplications] ([OriginProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_PermitApplications_OriginRegencyId] ON [PermitApplications] ([OriginRegencyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_LivestockQuotas_ProvinceCode] ON [LivestockQuotas] ([ProvinceCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LivestockQuotas_Type_Province_Year] ON [LivestockQuotas] ([LivestockType], [ProvinceCode], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_LivestockQuotas_Year] ON [LivestockQuotas] ([Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Ports_Code] ON [Ports] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_Ports_ProvinceCode_Name] ON [Ports] ([ProvinceCode], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_QuotaUsages_LivestockQuotaId_Status] ON [QuotaUsages] ([LivestockQuotaId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    CREATE INDEX [IX_QuotaUsages_PermitApplicationId] ON [QuotaUsages] ([PermitApplicationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD CONSTRAINT [FK_PermitApplications_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD CONSTRAINT [FK_PermitApplications_Users_KepalaDinasId] FOREIGN KEY ([KepalaDinasId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    ALTER TABLE [PermitApplications] ADD CONSTRAINT [FK_PermitApplications_Users_VerifikatorId] FOREIGN KEY ([VerifikatorId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821032908_AddFiturBaruDanPerubahanSkema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250821032908_AddFiturBaruDanPerubahanSkema', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250821040242_New-Migration-August'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250821040242_New-Migration-August', N'8.0.0');
END;
GO

COMMIT;
GO

