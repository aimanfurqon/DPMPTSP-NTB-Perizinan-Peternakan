-- Script untuk disable email verification sementara
-- Jalankan script ini di Azure SQL Database

-- Update semua user yang sudah ada menjadi verified
UPDATE Users 
SET IsEmailVerified = 1 
WHERE IsEmailVerified = 0;

PRINT 'Semua user existing sudah di-set sebagai email verified';

-- Tampilkan status user
SELECT 
    Username,
    Email,
    IsEmailVerified,
    Role,
    IsActive
FROM Users
ORDER BY Id;
