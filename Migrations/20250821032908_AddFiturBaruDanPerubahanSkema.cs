using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PerizinanPeternakan.Migrations
{
    /// <inheritdoc />
    public partial class AddFiturBaruDanPerubahanSkema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe-drop FK kalau ada (idempoten)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PermitApplications_Users_AdminId')
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_AdminId];
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PermitApplications_Users_KepalaDinasId')
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_KepalaDinasId];
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PermitApplications_Users_VerifikatorId')
    ALTER TABLE [PermitApplications] DROP CONSTRAINT [FK_PermitApplications_Users_VerifikatorId];
");

            // ❌ HAPUS semua DeleteData(Users, Id=1..8) — ini yang bikin konflik FK
            // (sengaja tidak ada kode delete di sini)

            // --- kolom baru di Users ---
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpires",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationTokenExpires",
                table: "Users",
                type: "datetime2",
                nullable: true);

            // --- kolom baru di PermitDocuments ---
            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentDate",
                table: "PermitDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentDescription",
                table: "PermitDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "PermitDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // --- ubah nullable CompanyName/Address ---
            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "PermitApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyAddress",
                table: "PermitApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            // --- kolom baru di PermitApplications ---
            migrationBuilder.AddColumn<string>(
                name: "ApplicantType",
                table: "PermitApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationProvinceId",
                table: "PermitApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationRegencyId",
                table: "PermitApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginProvinceId",
                table: "PermitApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginRegencyId",
                table: "PermitApplications",
                type: "int",
                nullable: true);

            // --- tabel baru ---
            migrationBuilder.CreateTable(
                name: "LivestockQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LivestockType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProvinceCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalQuota = table.Column<int>(type: "int", nullable: false),
                    UsedQuota = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegulationReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LivestockQuotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProvinceCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuotaUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LivestockQuotaId = table.Column<int>(type: "int", nullable: false),
                    PermitApplicationId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotaUsages_LivestockQuotas_LivestockQuotaId",
                        column: x => x.LivestockQuotaId,
                        principalTable: "LivestockQuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotaUsages_PermitApplications_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalTable: "PermitApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // --- index ---
            migrationBuilder.CreateIndex(name: "IX_PermitDocuments_DocumentDate", table: "PermitDocuments", column: "DocumentDate");
            migrationBuilder.CreateIndex(name: "IX_PermitDocuments_DocumentNumber", table: "PermitDocuments", column: "DocumentNumber");
            migrationBuilder.CreateIndex(name: "IX_PermitDocuments_DocumentType", table: "PermitDocuments", column: "DocumentType");
            migrationBuilder.CreateIndex(name: "IX_PermitApplications_DestinationProvinceId", table: "PermitApplications", column: "DestinationProvinceId");
            migrationBuilder.CreateIndex(name: "IX_PermitApplications_DestinationRegencyId", table: "PermitApplications", column: "DestinationRegencyId");
            migrationBuilder.CreateIndex(name: "IX_PermitApplications_OriginProvinceId", table: "PermitApplications", column: "OriginProvinceId");
            migrationBuilder.CreateIndex(name: "IX_PermitApplications_OriginRegencyId", table: "PermitApplications", column: "OriginRegencyId");
            migrationBuilder.CreateIndex(name: "IX_LivestockQuotas_ProvinceCode", table: "LivestockQuotas", column: "ProvinceCode");
            migrationBuilder.CreateIndex(name: "IX_LivestockQuotas_Type_Province_Year", table: "LivestockQuotas", columns: new[] { "LivestockType", "ProvinceCode", "Year" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_LivestockQuotas_Year", table: "LivestockQuotas", column: "Year");
            migrationBuilder.CreateIndex(name: "IX_Ports_Code", table: "Ports", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Ports_ProvinceCode_Name", table: "Ports", columns: new[] { "ProvinceCode", "Name" });
            migrationBuilder.CreateIndex(name: "IX_QuotaUsages_LivestockQuotaId_Status", table: "QuotaUsages", columns: new[] { "LivestockQuotaId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_QuotaUsages_PermitApplicationId", table: "QuotaUsages", column: "PermitApplicationId");

            migrationBuilder.AddForeignKey(
       name: "FK_PermitApplications_Users_AdminId",
       table: "PermitApplications",
       column: "AdminId",
       principalTable: "Users",
       principalColumn: "Id",
       onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PermitApplications_Users_KepalaDinasId",
                table: "PermitApplications",
                column: "KepalaDinasId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PermitApplications_Users_VerifikatorId",
                table: "PermitApplications",
                column: "VerifikatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PermitApplications_Users_AdminId",
                table: "PermitApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PermitApplications_Users_KepalaDinasId",
                table: "PermitApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PermitApplications_Users_VerifikatorId",
                table: "PermitApplications");

            migrationBuilder.DropTable(
                name: "Ports");

            migrationBuilder.DropTable(
                name: "QuotaUsages");

            migrationBuilder.DropTable(
                name: "LivestockQuotas");

            migrationBuilder.DropIndex(
                name: "IX_PermitDocuments_DocumentDate",
                table: "PermitDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PermitDocuments_DocumentNumber",
                table: "PermitDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PermitDocuments_DocumentType",
                table: "PermitDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PermitApplications_DestinationProvinceId",
                table: "PermitApplications");

            migrationBuilder.DropIndex(
                name: "IX_PermitApplications_DestinationRegencyId",
                table: "PermitApplications");

            migrationBuilder.DropIndex(
                name: "IX_PermitApplications_OriginProvinceId",
                table: "PermitApplications");

            migrationBuilder.DropIndex(
                name: "IX_PermitApplications_OriginRegencyId",
                table: "PermitApplications");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpires",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationTokenExpires",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DocumentDate",
                table: "PermitDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentDescription",
                table: "PermitDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "PermitDocuments");

            migrationBuilder.DropColumn(
                name: "ApplicantType",
                table: "PermitApplications");

            migrationBuilder.DropColumn(
                name: "DestinationProvinceId",
                table: "PermitApplications");

            migrationBuilder.DropColumn(
                name: "DestinationRegencyId",
                table: "PermitApplications");

            migrationBuilder.DropColumn(
                name: "OriginProvinceId",
                table: "PermitApplications");

            migrationBuilder.DropColumn(
                name: "OriginRegencyId",
                table: "PermitApplications");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "PermitApplications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyAddress",
                table: "PermitApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Alamat", "Email", "IsActive", "NamaLengkap", "NoTelepon", "Password", "Role", "TanggalDaftar", "Username" },
                values: new object[,]
                {
                    { 1, "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram", "kepaladinas@dpmptsp-ntb.go.id", true, "Hj. Eva Dewiyani, SP", "081234567890", "$2a$11$BCGyEESCpVMrVerzreURR.3BZ9uitmtMiaoKJU2pslyMH65gh.xZ.", "KepalaDinas", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "kepaladinas" },
                    { 2, "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram", "admin1@dpmptsp-ntb.go.id", true, "Ahmad Admin, S.Pt", "081234567891", "$2a$11$roTC0yhCxb.55PJ/PSMz/uFYEQw00YB657Co8cj8d2M0BzqYFexse", "Admin", new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin1" },
                    { 3, "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram", "admin2@dpmptsp-ntb.go.id", true, "Siti Admin, S.Pt", "081234567893", "$2a$11$dlnS3JhriSvJcFWWOwX9GefAc2Lk3YbsrFk5VLOjYfHEV0oQOBF9m", "Admin", new DateTime(2024, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin2" },
                    { 4, "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram", "verifikator1@dpmptsp-ntb.go.id", true, "Budi Verifikator, S.Pt, M.Si", "081234567894", "$2a$11$Di196bGJ3yOV.whx3TkB.eCnmtV03M/9DIWDIuEzGZFCCU0HK9qZq", "Verifikator", new DateTime(2024, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "verifikator1" },
                    { 5, "Kantor DPMPTSP NTB, Jl. Udayana No. 4 Mataram", "verifikator2@dpmptsp-ntb.go.id", true, "Rina Verifikator, S.Pt", "081234567895", "$2a$11$ShNRaNjMF/IxhcOoZGrUuOxI6Pr41tJujx6EuPP5iy.gtenwe.bwi", "Verifikator", new DateTime(2024, 1, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "verifikator2" },
                    { 6, "Desa Suka Maju, Kec. Praya, Lombok Tengah", "user1@example.com", true, "Budi Peternak", "081234567896", "$2a$11$n6zBiQ8IgPdOHkghfqQGru2DMtzkqxjOSzUM53teW1UAO73CpmOQO", "User", new DateTime(2024, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "user1" },
                    { 7, "Desa Dena, Kec. Madapangga, Kab. Bima", "cvdena@example.com", true, "CV. DENA BERSAUDARA", "081234567897", "$2a$11$Xe5nSUyr88T3rG.Qe/OtD.hzXqtKFW5TQ3U9WYBcCxaOD6x/b3f5a", "User", new DateTime(2024, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "cvdena" },
                    { 8, "Jl. Peternakan No. 15, Mataram", "sarimakmur@example.com", true, "PT. Sari Makmur Ternak", "081234567898", "$2a$11$4UyeoBPsfBrqzMEbm25mMuqTLWWigAusH4PuyoSPwi.YRvZYbqCzG", "User", new DateTime(2024, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "sarimakmur" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PermitApplications_Users_AdminId",
                table: "PermitApplications",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermitApplications_Users_KepalaDinasId",
                table: "PermitApplications",
                column: "KepalaDinasId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermitApplications_Users_VerifikatorId",
                table: "PermitApplications",
                column: "VerifikatorId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
