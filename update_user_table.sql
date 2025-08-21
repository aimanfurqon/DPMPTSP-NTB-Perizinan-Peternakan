-- Script untuk menambahkan kolom yang hilang di tabel Users
-- Jalankan script ini di Azure SQL Database

-- Cek apakah kolom sudah ada sebelum menambahkan
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsEmailVerified')
BEGIN
    ALTER TABLE Users ADD IsEmailVerified BIT NOT NULL DEFAULT 0;
    PRINT 'Kolom IsEmailVerified berhasil ditambahkan';
END
ELSE
BEGIN
    PRINT 'Kolom IsEmailVerified sudah ada';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'VerificationToken')
BEGIN
    ALTER TABLE Users ADD VerificationToken NVARCHAR(MAX) NULL;
    PRINT 'Kolom VerificationToken berhasil ditambahkan';
END
ELSE
BEGIN
    PRINT 'Kolom VerificationToken sudah ada';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'VerificationTokenExpires')
BEGIN
    ALTER TABLE Users ADD VerificationTokenExpires DATETIME2 NULL;
    PRINT 'Kolom VerificationTokenExpires berhasil ditambahkan';
END
ELSE
BEGIN
    PRINT 'Kolom VerificationTokenExpires sudah ada';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordResetToken')
BEGIN
    ALTER TABLE Users ADD PasswordResetToken NVARCHAR(MAX) NULL;
    PRINT 'Kolom PasswordResetToken berhasil ditambahkan';
END
ELSE
BEGIN
    PRINT 'Kolom PasswordResetToken sudah ada';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ResetTokenExpires')
BEGIN
    ALTER TABLE Users ADD ResetTokenExpires DATETIME2 NULL;
    PRINT 'Kolom ResetTokenExpires berhasil ditambahkan';
END
ELSE
BEGIN
    PRINT 'Kolom ResetTokenExpires sudah ada';
END

-- Tampilkan struktur tabel setelah update
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Users' 
ORDER BY ORDINAL_POSITION;
