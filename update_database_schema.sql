-- Script untuk menambahkan kolom yang hilang di database
-- Jalankan script ini di Azure SQL Database

-- 1. Update tabel PermitApplications
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitApplications' AND COLUMN_NAME = 'ApplicantType')
BEGIN
    ALTER TABLE PermitApplications ADD ApplicantType NVARCHAR(20) NULL;
    PRINT 'Kolom ApplicantType berhasil ditambahkan ke tabel PermitApplications';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitApplications' AND COLUMN_NAME = 'DestinationProvinceId')
BEGIN
    ALTER TABLE PermitApplications ADD DestinationProvinceId INT NULL;
    PRINT 'Kolom DestinationProvinceId berhasil ditambahkan ke tabel PermitApplications';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitApplications' AND COLUMN_NAME = 'DestinationRegencyId')
BEGIN
    ALTER TABLE PermitApplications ADD DestinationRegencyId INT NULL;
    PRINT 'Kolom DestinationRegencyId berhasil ditambahkan ke tabel PermitApplications';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitApplications' AND COLUMN_NAME = 'OriginProvinceId')
BEGIN
    ALTER TABLE PermitApplications ADD OriginProvinceId INT NULL;
    PRINT 'Kolom OriginProvinceId berhasil ditambahkan ke tabel PermitApplications';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitApplications' AND COLUMN_NAME = 'OriginRegencyId')
BEGIN
    ALTER TABLE PermitApplications ADD OriginRegencyId INT NULL;
    PRINT 'Kolom OriginRegencyId berhasil ditambahkan ke tabel PermitApplications';
END

-- 2. Update tabel PermitDocuments
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitDocuments' AND COLUMN_NAME = 'DocumentDate')
BEGIN
    ALTER TABLE PermitDocuments ADD DocumentDate DATETIME2 NULL;
    PRINT 'Kolom DocumentDate berhasil ditambahkan ke tabel PermitDocuments';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitDocuments' AND COLUMN_NAME = 'DocumentDescription')
BEGIN
    ALTER TABLE PermitDocuments ADD DocumentDescription NVARCHAR(500) NULL;
    PRINT 'Kolom DocumentDescription berhasil ditambahkan ke tabel PermitDocuments';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PermitDocuments' AND COLUMN_NAME = 'DocumentNumber')
BEGIN
    ALTER TABLE PermitDocuments ADD DocumentNumber NVARCHAR(100) NULL;
    PRINT 'Kolom DocumentNumber berhasil ditambahkan ke tabel PermitDocuments';
END

-- 3. Tampilkan struktur tabel setelah update
PRINT '=== STRUKTUR TABEL PERMITAPPLICATIONS ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PermitApplications' 
ORDER BY ORDINAL_POSITION;

PRINT '=== STRUKTUR TABEL PERMITDOCUMENTS ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PermitDocuments' 
ORDER BY ORDINAL_POSITION;

PRINT '=== STRUKTUR TABEL USERS ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
ORDER BY ORDINAL_POSITION;
