using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PerizinanPeternakan.Migrations
{
    /// <inheritdoc />
    public partial class NewApprovalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NamaLengkap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoTelepon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TanggalDaftar = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermitApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeparturePort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArrivalPort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminId = table.Column<int>(type: "int", nullable: true),
                    AdminApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifikatorId = table.Column<int>(type: "int", nullable: true),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KepalaDinasId = table.Column<int>(type: "int", nullable: true),
                    FinalApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedDocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermitApplications_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermitApplications_Users_KepalaDinasId",
                        column: x => x.KepalaDinasId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermitApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PermitApplications_Users_VerifikatorId",
                        column: x => x.VerifikatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LivestockDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitApplicationId = table.Column<int>(type: "int", nullable: false),
                    LivestockType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LivestockDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LivestockDetails_PermitApplications_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalTable: "PermitApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermitApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitApplicationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermitApprovalHistories_PermitApplications_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalTable: "PermitApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermitApprovalHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermitDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitApplicationId = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermitDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermitDocuments_PermitApplications_PermitApplicationId",
                        column: x => x.PermitApplicationId,
                        principalTable: "PermitApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermitDocuments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_LivestockDetails_PermitApplicationId",
                table: "LivestockDetails",
                column: "PermitApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_AdminId",
                table: "PermitApplications",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_ApplicationNumber",
                table: "PermitApplications",
                column: "ApplicationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_KepalaDinasId",
                table: "PermitApplications",
                column: "KepalaDinasId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_Status",
                table: "PermitApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_SubmissionDate",
                table: "PermitApplications",
                column: "SubmissionDate");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_UserId",
                table: "PermitApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApplications_VerifikatorId",
                table: "PermitApplications",
                column: "VerifikatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalHistories_ActionDate",
                table: "PermitApprovalHistories",
                column: "ActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalHistories_PermitApplicationId",
                table: "PermitApprovalHistories",
                column: "PermitApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitApprovalHistories_UserId",
                table: "PermitApprovalHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitDocuments_PermitApplicationId",
                table: "PermitDocuments",
                column: "PermitApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PermitDocuments_UploadedByUserId",
                table: "PermitDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LivestockDetails");

            migrationBuilder.DropTable(
                name: "PermitApprovalHistories");

            migrationBuilder.DropTable(
                name: "PermitDocuments");

            migrationBuilder.DropTable(
                name: "PermitApplications");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
