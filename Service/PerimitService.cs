using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;
using System.Text;

namespace PerizinanPeternakan.Services
{
    public class PermitService : IPermitService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly ILogger<PermitService> _logger;

        public PermitService(
            ApplicationDbContext context,
            IDocumentService documentService,
            IPdfGeneratorService pdfGenerator,
            ILogger<PermitService> logger)
        {
            _context = context;
            _documentService = documentService;
            _pdfGenerator = pdfGenerator;
            _logger = logger;
        }

        public async Task<List<PermitListViewModel>> GetPermitsForUserAsync(string userRole, int userId)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);

                return await query
                    .Include(p => p.User)
                    .Include(p => p.Documents)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        AdminApprovalDate = p.AdminApprovalDate,
                        VerificationDate = p.VerificationDate,
                        FinalApprovalDate = p.FinalApprovalDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = p.Status == PermitStatus.FinalApproved &&
                                     !string.IsNullOrEmpty(p.GeneratedDocumentPath) &&
                                     userRole == "User",
                        CanView = true,
                        CanApprove = CanUserApprove(userRole, p.Status),
                        GeneratedDocumentPath = p.GeneratedDocumentPath,
                        CurrentApprovalLevel = p.CurrentApprovalLevel,
                        DocumentCount = p.Documents.Count,
                        HasAllRequiredDocuments = p.Documents.Count >= 7
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permits for user {UserId} with role {UserRole}", userId, userRole);
                return new List<PermitListViewModel>();
            }
        }

        public async Task<PermitDetailViewModel?> GetPermitDetailAsync(int permitId, string userRole, int userId)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.ApprovalHistory)
                        .ThenInclude(h => h.User)
                    .Include(p => p.Admin)
                    .Include(p => p.Verifikator)
                    .Include(p => p.KepalaDinas)
                    .Include(p => p.Documents)
                        .ThenInclude(d => d.UploadedByUser)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null) return null;

                // Check access rights
                if (userRole == "User" && permit.UserId != userId)
                {
                    return null;
                }

                return new PermitDetailViewModel
                {
                    Id = permit.Id,
                    ApplicationNumber = permit.ApplicationNumber,
                    CompanyName = permit.CompanyName,
                    CompanyAddress = permit.CompanyAddress,
                    ApplicantName = permit.User.NamaLengkap,
                    ApplicantEmail = permit.User.Email,
                    ApplicantPhone = permit.User.NoTelepon,
                    Status = permit.Status,
                    SubmissionDate = permit.SubmissionDate,
                    AdminApprovalDate = permit.AdminApprovalDate,
                    VerificationDate = permit.VerificationDate,
                    FinalApprovalDate = permit.FinalApprovalDate,
                    OriginLocation = permit.OriginLocation,
                    DestinationLocation = permit.DestinationLocation,
                    DeparturePort = permit.DeparturePort,
                    ArrivalPort = permit.ArrivalPort,
                    RejectionReason = permit.RejectionReason,
                    ValidFrom = permit.ValidFrom,
                    ValidUntil = permit.ValidUntil,
                    GeneratedDocumentPath = permit.GeneratedDocumentPath,
                    AdminName = permit.Admin?.NamaLengkap,
                    VerifikatorName = permit.Verifikator?.NamaLengkap,
                    KepalaDinasName = permit.KepalaDinas?.NamaLengkap,
                    CurrentApprovalLevel = permit.CurrentApprovalLevel,
                    LivestockDetails = permit.LivestockDetails.Select(d => new LivestockDetailViewModel
                    {
                        LivestockType = d.LivestockType,
                        Quantity = d.Quantity,
                        Description = d.Description
                    }).ToList(),
                    ApprovalHistory = permit.ApprovalHistory.Select(h => new ApprovalHistoryViewModel
                    {
                        Action = h.Action,
                        ActionBy = h.User.NamaLengkap,
                        ActionByRole = h.User.Role,
                        ActionDate = h.ActionDate,
                        Comments = h.Comments,
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus
                    }).OrderBy(h => h.ActionDate).ToList(),
                    Documents = permit.Documents.Select(d => new DocumentViewModel
                    {
                        Id = d.Id,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType,
                        FilePath = d.FilePath,
                        FileSize = d.FileSize,
                        FileExtension = d.FileExtension,
                        UploadDate = d.UploadDate,
                        UploadedBy = d.UploadedByUser.NamaLengkap,
                        DocumentDate = d.DocumentDate,
                        DocumentNumber = d.DocumentNumber,
                        DocumentDescription = d.DocumentDescription
                    }).OrderBy(d => d.DocumentType).ToList(),
                    CanDownload = permit.Status == PermitStatus.FinalApproved &&
                                 !string.IsNullOrEmpty(permit.GeneratedDocumentPath),
                    CanApprove = CanUserApprove(userRole, permit.Status)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit detail for ID {PermitId}", permitId);
                return null;
            }
        }

        public async Task<(bool Success, string ErrorMessage, string ApplicationNumber)> CreatePermitAsync(
            PermitApplicationViewModel model, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("Starting permit creation process for user {UserId}", userId);

                // Validate the application
                var validation = ValidatePermitApplication(model);
                if (!validation.IsValid)
                {
                    return (false, string.Join("; ", validation.Errors), "");
                }

                // Get user information
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User tidak ditemukan", "");
                }

                // Generate application number
                var applicationNumber = await GenerateApplicationNumberAsync();

                // Create permit application
                var permitApplication = new LivestockPermitApplication
                {
                    ApplicationNumber = applicationNumber,
                    UserId = userId,
                    CompanyName = model.CompanyName.Trim(),
                    CompanyAddress = model.CompanyAddress.Trim(),
                    OriginLocation = model.OriginLocation?.Trim() ?? "",
                    DestinationLocation = model.DestinationLocation?.Trim() ?? "",
                    DeparturePort = model.DeparturePort?.Trim() ?? "",
                    ArrivalPort = model.ArrivalPort?.Trim() ?? "",
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now,
                    CurrentApprovalLevel = 1,
                    OriginProvinceId = model.OriginProvinceId,
                    OriginRegencyId = model.OriginRegencyId,
                    DestinationProvinceId = model.DestinationProvinceId,
                    DestinationRegencyId = model.DestinationRegencyId
                };

                // Add livestock details
                foreach (var livestockDetail in model.LivestockDetails
                    .Where(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
                {
                    permitApplication.LivestockDetails.Add(new LivestockDetail
                    {
                        LivestockType = livestockDetail.LivestockType.Trim(),
                        Quantity = livestockDetail.Quantity,
                        Description = livestockDetail.Description?.Trim()
                    });
                }

                // Save permit application first
                _context.PermitApplications.Add(permitApplication);
                await _context.SaveChangesAsync();

                // Upload supporting documents
                var uploadResult = await _documentService.UploadSupportingDocumentsAsync(
                    permitApplication.Id, model, userId);

                if (!uploadResult.Success)
                {
                    await transaction.RollbackAsync();
                    return (false, uploadResult.ErrorMessage, "");
                }

                // Add approval history entry
                var approvalHistory = new PermitApprovalHistory
                {
                    PermitApplicationId = permitApplication.Id,
                    UserId = userId,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan izin diajukan oleh pemohon",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(approvalHistory);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Permit creation completed successfully with number {ApplicationNumber}",
                    applicationNumber);

                return (true, "", applicationNumber);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating permit for user {UserId}", userId);
                return (false, "Terjadi kesalahan saat menyimpan permohonan. Silakan coba lagi.", "");
            }
        }

        public async Task<string> GenerateApplicationNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Lock table untuk mencegah race condition
                await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PermitApplications WITH (TABLOCKX)");

                var lastApplication = await _context.PermitApplications
                    .Where(p => p.ApplicationNumber.EndsWith($"/03-260/DPM&PTSP/{year}") &&
                               p.SubmissionDate.Year == year &&
                               p.SubmissionDate.Month == month)
                    .OrderByDescending(p => p.ApplicationNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;

                if (lastApplication != null)
                {
                    var numberPart = lastApplication.ApplicationNumber.Split('/')[0];
                    if (int.TryParse(numberPart, out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }

                string applicationNumber;
                int maxRetries = 10;
                int retryCount = 0;

                do
                {
                    applicationNumber = $"{nextNumber.ToString().PadLeft(3, '0')}/03-260/DPM&PTSP/{year}";

                    var exists = await _context.PermitApplications
                        .AnyAsync(p => p.ApplicationNumber == applicationNumber);

                    if (!exists)
                    {
                        break;
                    }

                    nextNumber++;
                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        throw new InvalidOperationException(
                            "Tidak dapat menggenerate nomor aplikasi unik setelah beberapa percobaan");
                    }

                } while (true);

                await transaction.CommitAsync();
                return applicationNumber;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> ApprovePermitAsync(
            int permitId, string action, string comments, int userId, string userRole)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return (false, "Permohonan tidak ditemukan");
                }

                if (!CanUserApprove(userRole, permit.Status))
                {
                    return (false, "Permohonan tidak dapat diproses pada tahap ini");
                }

                // Validate documents for admin approval
                if (userRole == "Admin" && action == "Approve" && permit.Documents.Count < 7)
                {
                    return (false, "Dokumen pendukung belum lengkap. Permohonan tidak dapat disetujui.");
                }

                var result = await ProcessApproval(permit, action, comments, userId, userRole);
                if (result.Success)
                {
                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving permit {PermitId} by user {UserId}", permitId, userId);
                return (false, "Terjadi kesalahan saat memproses approval");
            }
        }

        public bool CanUserApprove(string userRole, PermitStatus status)
        {
            return userRole switch
            {
                "Admin" => status == PermitStatus.Submitted || status == PermitStatus.UnderAdminReview,
                "Verifikator" => status == PermitStatus.AdminApproved || status == PermitStatus.UnderVerifikatorReview,
                "KepalaDinas" => status == PermitStatus.VerifikatorApproved || status == PermitStatus.PendingKepalaDinas,
                _ => false
            };
        }

        public async Task<(bool Success, string ErrorMessage)> GeneratePermitDocumentAsync(int permitId)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.Admin)
                    .Include(p => p.Verifikator)
                    .Include(p => p.KepalaDinas)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return (false, "Permit tidak ditemukan");
                }

                var htmlBytes = await _pdfGenerator.GeneratePermitPdf(permit);

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "permits");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"permit_{permit.ApplicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.html";
                var filePath = Path.Combine(uploadsPath, fileName);

                await File.WriteAllBytesAsync(filePath, htmlBytes);

                permit.GeneratedDocumentPath = $"/documents/permits/{fileName}";
                await _context.SaveChangesAsync();

                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating document for permit {PermitId}", permitId);
                return (false, "Gagal generate dokumen");
            }
        }

        public async Task<string> GetDocumentPathAsync(int permitId)
        {
            try
            {
                var permit = await _context.PermitApplications.FindAsync(permitId);
                return permit?.GeneratedDocumentPath ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document path for permit {PermitId}", permitId);
                return "";
            }
        }

        // Implementation untuk methods lainnya...
        // (GetPermitStatisticsAsync, GetDashboardDataAsync, dll.)

        #region Private Helper Methods

        private IQueryable<LivestockPermitApplication> GetPermitsQuery(string userRole, int userId)
        {
            var baseQuery = _context.PermitApplications.AsQueryable();

            return userRole switch
            {
                "User" => baseQuery.Where(p => p.UserId == userId),
                "Admin" => baseQuery.Where(p => p.Status == PermitStatus.Submitted ||
                                              p.Status == PermitStatus.UnderAdminReview),
                "Verifikator" => baseQuery.Where(p => p.Status == PermitStatus.AdminApproved ||
                                                    p.Status == PermitStatus.UnderVerifikatorReview),
                "KepalaDinas" => baseQuery.Where(p => p.Status == PermitStatus.VerifikatorApproved ||
                                                    p.Status == PermitStatus.PendingKepalaDinas),
                _ => baseQuery.Where(p => false)
            };
        }

        private async Task<(bool Success, string ErrorMessage)> ProcessApproval(
            LivestockPermitApplication permit, string action, string comments, int userId, string userRole)
        {
            try
            {
                var fromStatus = permit.Status;
                PermitStatus toStatus;
                string actionText;

                if (action == "Approve")
                {
                    if (userRole == "Admin")
                    {
                        toStatus = PermitStatus.AdminApproved;
                        actionText = "Disetujui Admin";
                        permit.AdminId = userId;
                        permit.AdminApprovalDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 2;

                        // Generate PDF document setelah admin approve
                        await GeneratePermitDocumentAsync(permit.Id);
                    }
                    else if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorApproved;
                        actionText = "Disetujui Verifikator";
                        permit.VerifikatorId = userId;
                        permit.VerificationDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 3;
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.FinalApproved;
                        actionText = "Disetujui Kepala Dinas";
                        permit.KepalaDinasId = userId;
                        permit.FinalApprovalDate = DateTime.Now;
                        permit.ValidFrom = DateTime.Now;
                        permit.ValidUntil = DateTime.Now.AddMonths(6);
                        permit.CurrentApprovalLevel = 4;
                    }
                }
                else // Reject
                {
                    if (userRole == "Admin")
                    {
                        toStatus = PermitStatus.AdminRejected;
                        actionText = "Ditolak Admin";
                    }
                    else if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorRejected;
                        actionText = "Ditolak Verifikator";
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.KepalaDinasRejected;
                        actionText = "Ditolak Kepala Dinas";
                    }

                    permit.RejectionReason = comments;
                }

                permit.Status = toStatus;

                // Add approval history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = userId,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = actionText,
                    Comments = comments ?? $"Bulk {action.ToLower()}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval for permit {PermitId}", permit.Id);
                return (false, ex.Message);
            }
        }

        public (bool IsValid, List<string> Errors) ValidatePermitApplication(PermitApplicationViewModel model)
        {
            var errors = new List<string>();

            // Validate basic fields
            if (string.IsNullOrWhiteSpace(model.CompanyName))
                errors.Add("Nama perusahaan harus diisi");

            if (string.IsNullOrWhiteSpace(model.CompanyAddress))
                errors.Add("Alamat perusahaan harus diisi");

            // Validate livestock details
            if (model.LivestockDetails == null ||
                !model.LivestockDetails.Any(d => !string.IsNullOrEmpty(d.LivestockType) && d.Quantity > 0))
            {
                errors.Add("Minimal harus ada satu detail ternak yang valid");
            }

            // Validate documents
            var docValidation = _documentService.ValidateAllDocumentsUploaded(model);
            if (!docValidation)
            {
                errors.Add("Semua dokumen wajib harus diupload");
            }

            // Validate document details
            var detailValidation = _documentService.ValidateDocumentDetails(model);
            if (!detailValidation)
            {
                errors.Add("Detail dokumen (tanggal dan nomor) harus diisi dengan benar");
            }

            return (!errors.Any(), errors);
        }

        public (bool IsValid, List<string> Errors) ValidateRequiredDocuments(PermitApplicationViewModel model)
        {
            return _documentService.ValidateAllDocumentsUploaded(model)
                ? (true, new List<string>())
                : (false, new List<string> { "Dokumen wajib belum lengkap" });
        }

        public async Task<object> GetPermitStatisticsAsync(string userRole, int userId)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);
                var permits = await query.ToListAsync();

                return new
                {
                    total = permits.Count,
                    pending = permits.Count(p => p.Status == PermitStatus.Submitted ||
                                               p.Status == PermitStatus.UnderAdminReview ||
                                               p.Status == PermitStatus.UnderVerifikatorReview ||
                                               p.Status == PermitStatus.PendingKepalaDinas),
                    approved = permits.Count(p => p.Status == PermitStatus.FinalApproved),
                    rejected = permits.Count(p => p.Status == PermitStatus.AdminRejected ||
                                                p.Status == PermitStatus.VerifikatorRejected ||
                                                p.Status == PermitStatus.KepalaDinasRejected ||
                                                p.Status == PermitStatus.FinalRejected),
                    inProcess = permits.Count(p => p.Status == PermitStatus.AdminApproved ||
                                                 p.Status == PermitStatus.VerifikatorApproved),

                    // Monthly statistics
                    thisMonth = permits.Count(p => p.SubmissionDate.Month == DateTime.Now.Month &&
                                                 p.SubmissionDate.Year == DateTime.Now.Year),
                    thisWeek = permits.Count(p => p.SubmissionDate >= DateTime.Now.AddDays(-7)),
                    today = permits.Count(p => p.SubmissionDate.Date == DateTime.Today),

                    // Average processing time
                    avgProcessingDays = CalculateAverageProcessingTime(permits),

                    // Status distribution
                    statusDistribution = permits
                        .GroupBy(p => p.Status)
                        .Select(g => new
                        {
                            status = g.Key.ToString(),
                            statusText = PermitStatusHelper.GetStatusText(g.Key),
                            count = g.Count(),
                            percentage = Math.Round((double)g.Count() / permits.Count * 100, 1)
                        })
                        .OrderByDescending(x => x.count)
                        .ToList(),

                    // Recent activity
                    recentActivity = permits
                        .OrderByDescending(p => p.SubmissionDate)
                        .Take(10)
                        .Select(p => new
                        {
                            id = p.Id,
                            applicationNumber = p.ApplicationNumber,
                            companyName = p.CompanyName,
                            status = PermitStatusHelper.GetStatusText(p.Status),
                            statusClass = PermitStatusHelper.GetStatusClass(p.Status),
                            submissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                            daysAgo = (DateTime.Now - p.SubmissionDate).Days
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit statistics for user {UserId}", userId);
                return new { error = ex.Message };
            }
        }

        public async Task<object> GetDashboardDataAsync(string userRole, int userId)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);
                var permits = await query.Include(p => p.User).ToListAsync();

                var dashboardData = new
                {
                    // Summary cards
                    summary = new
                    {
                        totalPermits = permits.Count,
                        pendingAction = permits.Count(p => CanUserApprove(userRole, p.Status)),
                        approvedToday = permits.Count(p => p.Status == PermitStatus.FinalApproved &&
                                                         p.FinalApprovalDate?.Date == DateTime.Today),
                        pendingReview = permits.Count(p => IsPendingStatus(p.Status))
                    },

                    // Recent activity
                    recentActivity = permits
                        .OrderByDescending(p => GetLastActivityDate(p))
                        .Take(5)
                        .Select(p => new
                        {
                            id = p.Id,
                            applicationNumber = p.ApplicationNumber,
                            companyName = p.CompanyName,
                            applicantName = p.User.NamaLengkap,
                            status = PermitStatusHelper.GetStatusText(p.Status),
                            statusClass = PermitStatusHelper.GetStatusClass(p.Status),
                            lastActivity = GetLastActivityDate(p).ToString("dd/MM/yyyy HH:mm"),
                            daysAgo = (DateTime.Now - GetLastActivityDate(p)).Days,
                            canApprove = CanUserApprove(userRole, p.Status)
                        }),

                    // Status distribution for chart
                    statusChart = permits
                        .GroupBy(p => p.Status)
                        .Select(g => new
                        {
                            label = PermitStatusHelper.GetStatusText(g.Key),
                            value = g.Count(),
                            color = GetStatusColor(g.Key),
                            percentage = Math.Round((double)g.Count() / permits.Count * 100, 1)
                        })
                        .Where(x => x.value > 0)
                        .OrderByDescending(x => x.value)
                        .ToList(),

                    // Monthly trend for the last 6 months
                    monthlyTrend = GetMonthlyTrend(permits),

                    // Performance metrics
                    performance = new
                    {
                        avgProcessingDays = CalculateAverageProcessingTime(permits),
                        completionRate = permits.Count > 0 ?
                            Math.Round((double)permits.Count(p => p.Status == PermitStatus.FinalApproved) / permits.Count * 100, 1) : 0,
                        rejectionRate = permits.Count > 0 ?
                            Math.Round((double)permits.Count(p => IsRejectedStatus(p.Status)) / permits.Count * 100, 1) : 0,
                        averageDocuments = permits.Any() ? Math.Round(permits.Average(p => p.Documents.Count), 1) : 0
                    },

                    // Quick actions based on user role
                    quickActions = GetQuickActionsForUser(userRole, permits),

                    // Urgent items that need attention
                    urgentItems = GetUrgentItems(permits, userRole)
                };

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for user {UserId}", userId);
                return new { error = ex.Message };
            }
        }

        public async Task<List<object>> GetMonthlyTrendAsync(string userRole, int userId, int months = 6)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);
                var permits = await query.ToListAsync();

                return GetMonthlyTrend(permits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly trend for user {UserId}", userId);
                return new List<object>();
            }
        }

        #region Private Helper Methods for Dashboard

        private bool IsPendingStatus(PermitStatus status)
        {
            return status == PermitStatus.Submitted ||
                   status == PermitStatus.UnderAdminReview ||
                   status == PermitStatus.UnderVerifikatorReview ||
                   status == PermitStatus.PendingKepalaDinas;
        }

        private bool IsRejectedStatus(PermitStatus status)
        {
            return status == PermitStatus.AdminRejected ||
                   status == PermitStatus.VerifikatorRejected ||
                   status == PermitStatus.KepalaDinasRejected ||
                   status == PermitStatus.FinalRejected;
        }

        private DateTime GetLastActivityDate(LivestockPermitApplication permit)
        {
            return permit.FinalApprovalDate ??
                   permit.VerificationDate ??
                   permit.AdminApprovalDate ??
                   permit.SubmissionDate;
        }

        private string GetStatusColor(PermitStatus status)
        {
            return status switch
            {
                PermitStatus.Draft => "#6c757d",
                PermitStatus.Submitted => "#ffc107",
                PermitStatus.UnderAdminReview => "#17a2b8",
                PermitStatus.AdminApproved => "#007bff",
                PermitStatus.AdminRejected => "#dc3545",
                PermitStatus.UnderVerifikatorReview => "#17a2b8",
                PermitStatus.VerifikatorApproved => "#007bff",
                PermitStatus.VerifikatorRejected => "#dc3545",
                PermitStatus.PendingKepalaDinas => "#17a2b8",
                PermitStatus.FinalApproved => "#28a745",
                PermitStatus.KepalaDinasRejected => "#dc3545",
                PermitStatus.FinalRejected => "#dc3545",
                _ => "#6c757d"
            };
        }

        private object GetQuickActionsForUser(string userRole, List<LivestockPermitApplication> permits)
        {
            return userRole switch
            {
                "User" => new
                {
                    canCreateNew = true,
                    hasAnyActive = permits.Any(p => !IsRejectedStatus(p.Status) && p.Status != PermitStatus.FinalApproved),
                    canDownload = permits.Count(p => p.Status == PermitStatus.FinalApproved),
                    hasRejected = permits.Count(p => IsRejectedStatus(p.Status))
                },
                "Admin" => new
                {
                    pendingReview = permits.Count(p => p.Status == PermitStatus.Submitted),
                    canBulkProcess = permits.Count(p => CanUserApprove(userRole, p.Status)) > 1,
                    avgProcessingTime = CalculateAverageProcessingTime(permits.Where(p => p.AdminId != null).ToList()),
                    todayTarget = 5 // Could be configurable
                },
                "Verifikator" => new
                {
                    pendingVerification = permits.Count(p => p.Status == PermitStatus.AdminApproved),
                    verifiedToday = permits.Count(p => p.VerificationDate?.Date == DateTime.Today),
                    canBulkProcess = permits.Count(p => CanUserApprove(userRole, p.Status)) > 1,
                    avgVerificationTime = CalculateAverageVerificationTime(permits)
                },
                "KepalaDinas" => new
                {
                    pendingFinalApproval = permits.Count(p => p.Status == PermitStatus.VerifikatorApproved),
                    approvedToday = permits.Count(p => p.FinalApprovalDate?.Date == DateTime.Today),
                    canBulkProcess = permits.Count(p => CanUserApprove(userRole, p.Status)) > 1,
                    monthlyTarget = 50 // Could be configurable
                },
                _ => new { }
            };
        }

        private List<object> GetUrgentItems(List<LivestockPermitApplication> permits, string userRole)
        {
            var urgentItems = new List<object>();

            // Items pending for more than expected time
            var urgentPermits = permits.Where(p =>
            {
                var daysSinceSubmission = (DateTime.Now - p.SubmissionDate).Days;
                return userRole switch
                {
                    "Admin" => p.Status == PermitStatus.Submitted && daysSinceSubmission > 3,
                    "Verifikator" => p.Status == PermitStatus.AdminApproved && daysSinceSubmission > 7,
                    "KepalaDinas" => p.Status == PermitStatus.VerifikatorApproved && daysSinceSubmission > 10,
                    _ => false
                };
            }).Take(5);

            foreach (var permit in urgentPermits)
            {
                var daysPending = (DateTime.Now - GetLastActivityDate(permit)).Days;
                urgentItems.Add(new
                {
                    id = permit.Id,
                    applicationNumber = permit.ApplicationNumber,
                    companyName = permit.CompanyName,
                    daysPending = daysPending,
                    urgencyLevel = daysPending > 7 ? "high" : daysPending > 3 ? "medium" : "low",
                    status = PermitStatusHelper.GetStatusText(permit.Status)
                });
            }

            return urgentItems;
        }

        private double CalculateAverageVerificationTime(List<LivestockPermitApplication> permits)
        {
            var verifiedPermits = permits.Where(p =>
                p.VerificationDate.HasValue && p.AdminApprovalDate.HasValue).ToList();

            if (!verifiedPermits.Any()) return 0;

            var totalDays = verifiedPermits.Sum(p =>
                (p.VerificationDate!.Value - p.AdminApprovalDate!.Value).TotalDays);

            return Math.Round(totalDays / verifiedPermits.Count, 1);
        }

        #endregion

        public async Task<(List<object> Data, int TotalCount)> GetPermitsDataAsync(
            string userRole, int userId, int start, int length, string search,
            string statusFilter, string dateFilter)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(search) ||
                        p.CompanyName.Contains(search) ||
                        p.User.NamaLengkap.Contains(search) ||
                        p.OriginLocation.Contains(search) ||
                        p.DestinationLocation.Contains(search));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<PermitStatus>(statusFilter, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(dateFilter) && DateTime.TryParse(dateFilter, out var filterDate))
                {
                    query = query.Where(p => p.SubmissionDate.Date == filterDate.Date);
                }

                var totalRecords = await query.CountAsync();

                var permits = await query
                    .Include(p => p.User)
                    .Include(p => p.Documents)
                    .OrderByDescending(p => p.SubmissionDate)
                    .Skip(start)
                    .Take(length)
                    .Select(p => new
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status.ToString(),
                        StatusText = PermitStatusHelper.GetStatusText(p.Status),
                        StatusClass = PermitStatusHelper.GetStatusClass(p.Status),
                        ProgressPercentage = PermitStatusHelper.GetProgressPercentage(p.Status),
                        ProgressText = PermitStatusHelper.GetProgressText(p.Status),
                        SubmissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                        SubmissionTime = p.SubmissionDate.ToString("HH:mm"),
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        DocumentCount = p.Documents.Count,
                        CanApprove = CanUserApprove(userRole, p.Status),
                        CanDownload = p.Status == PermitStatus.FinalApproved &&
                                     !string.IsNullOrEmpty(p.GeneratedDocumentPath) && userRole == "User",
                        HasDocument = !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        GeneratedDocumentPath = p.GeneratedDocumentPath
                    })
                    .ToListAsync();

                return (permits.Cast<object>().ToList(), totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permits data for user {UserId}", userId);
                return (new List<object>(), 0);
            }
        }

        public async Task<List<object>> AdvancedSearchAsync(AdvancedSearchRequest request, string userRole, int userId)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);

                // Apply advanced filters
                if (!string.IsNullOrEmpty(request.ApplicationNumber))
                    query = query.Where(p => p.ApplicationNumber.Contains(request.ApplicationNumber));

                if (!string.IsNullOrEmpty(request.CompanyName))
                    query = query.Where(p => p.CompanyName.Contains(request.CompanyName));

                if (!string.IsNullOrEmpty(request.ApplicantName))
                    query = query.Where(p => p.User.NamaLengkap.Contains(request.ApplicantName));

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PermitStatus>(request.Status, out var status))
                    query = query.Where(p => p.Status == status);

                if (!string.IsNullOrEmpty(request.OriginLocation))
                    query = query.Where(p => p.OriginLocation.Contains(request.OriginLocation));

                if (!string.IsNullOrEmpty(request.DestinationLocation))
                    query = query.Where(p => p.DestinationLocation.Contains(request.DestinationLocation));

                if (request.DateFrom.HasValue)
                    query = query.Where(p => p.SubmissionDate >= request.DateFrom.Value);

                if (request.DateTo.HasValue)
                    query = query.Where(p => p.SubmissionDate <= request.DateTo.Value.AddDays(1));

                if (request.MinDocuments.HasValue)
                    query = query.Where(p => p.Documents.Count >= request.MinDocuments.Value);

                var results = await query
                    .Include(p => p.User)
                    .Include(p => p.Documents)
                    .OrderByDescending(p => p.SubmissionDate)
                    .Take(100)
                    .Select(p => new
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = PermitStatusHelper.GetStatusText(p.Status),
                        SubmissionDate = p.SubmissionDate.ToString("dd/MM/yyyy"),
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        DocumentCount = p.Documents.Count
                    })
                    .ToListAsync();

                return results.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced search for user {UserId}", userId);
                return new List<object>();
            }
        }

        public async Task<(bool Success, string Message, int SuccessCount, List<string> Errors)> BulkApproveAsync(
            List<int> permitIds, string comments, int userId, string userRole)
        {
            try
            {
                var permits = await _context.PermitApplications
                    .Where(p => permitIds.Contains(p.Id))
                    .ToListAsync();

                var successCount = 0;
                var errors = new List<string>();

                foreach (var permit in permits)
                {
                    if (CanUserApprove(userRole, permit.Status))
                    {
                        var result = await ProcessApproval(permit, "Approve", comments, userId, userRole);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{permit.ApplicationNumber}: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        errors.Add($"{permit.ApplicationNumber}: Tidak dapat diproses pada tahap ini");
                    }
                }

                await _context.SaveChangesAsync();

                return (true, $"{successCount} permohonan berhasil disetujui", successCount, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approve by user {UserId}", userId);
                return (false, $"Terjadi kesalahan: {ex.Message}", 0, new List<string>());
            }
        }

        public async Task<(bool Success, string Message, int SuccessCount, List<string> Errors)> BulkRejectAsync(
            List<int> permitIds, string comments, int userId, string userRole)
        {
            try
            {
                var permits = await _context.PermitApplications
                    .Where(p => permitIds.Contains(p.Id))
                    .ToListAsync();

                var successCount = 0;
                var errors = new List<string>();

                foreach (var permit in permits)
                {
                    if (CanUserApprove(userRole, permit.Status))
                    {
                        var result = await ProcessApproval(permit, "Reject", comments, userId, userRole);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{permit.ApplicationNumber}: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        errors.Add($"{permit.ApplicationNumber}: Tidak dapat diproses pada tahap ini");
                    }
                }

                await _context.SaveChangesAsync();

                return (true, $"{successCount} permohonan berhasil ditolak", successCount, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk reject by user {UserId}", userId);
                return (false, $"Terjadi kesalahan: {ex.Message}", 0, new List<string>());
            }
        }

        public async Task<byte[]> ExportToCsvAsync(string userRole, int userId, string statusFilter,
            string dateFrom, string dateTo, string search)
        {
            try
            {
                var query = GetPermitsQuery(userRole, userId);

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.ApplicationNumber.Contains(search) ||
                        p.CompanyName.Contains(search) ||
                        p.User.NamaLengkap.Contains(search));
                }

                if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<PermitStatus>(statusFilter, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                {
                    query = query.Where(p => p.SubmissionDate >= fromDate);
                }

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                {
                    query = query.Where(p => p.SubmissionDate <= toDate.AddDays(1));
                }

                var permits = await query
                    .Include(p => p.User)
                    .Include(p => p.Documents)
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("No. Aplikasi,Perusahaan,Pemohon,Status,Tanggal Pengajuan,Asal,Tujuan,Jumlah Dokumen,Persetujuan Admin,Verifikasi,Persetujuan Final");

                foreach (var permit in permits)
                {
                    csv.AppendLine($"\"{permit.ApplicationNumber}\",\"{permit.CompanyName}\",\"{permit.User.NamaLengkap}\",\"{PermitStatusHelper.GetStatusText(permit.Status)}\",\"{permit.SubmissionDate:dd/MM/yyyy HH:mm}\",\"{permit.OriginLocation}\",\"{permit.DestinationLocation}\",\"{permit.Documents.Count}\",\"{(permit.AdminApprovalDate?.ToString("dd/MM/yyyy HH:mm") ?? "")}\",\"{(permit.VerificationDate?.ToString("dd/MM/yyyy HH:mm") ?? "")}\",\"{(permit.FinalApprovalDate?.ToString("dd/MM/yyyy HH:mm") ?? "")}\"");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV for user {UserId}", userId);
                return Encoding.UTF8.GetBytes("Error occurred during export");
            }
        }

        #region Private Helper Methods

        private double CalculateAverageProcessingTime(List<LivestockPermitApplication> permits)
        {
            var completedPermits = permits.Where(p =>
                p.Status == PermitStatus.FinalApproved &&
                p.FinalApprovalDate.HasValue).ToList();

            if (!completedPermits.Any()) return 0;

            var totalDays = completedPermits.Sum(p =>
                (p.FinalApprovalDate!.Value - p.SubmissionDate).TotalDays);

            return Math.Round(totalDays / completedPermits.Count, 1);
        }

        private List<object> GetMonthlyTrend(List<LivestockPermitApplication> permits)
        {
            var monthlyData = permits
                .Where(p => p.SubmissionDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(p => new { p.SubmissionDate.Year, p.SubmissionDate.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    submitted = g.Count(),
                    approved = g.Count(p => p.Status == PermitStatus.FinalApproved),
                    rejected = g.Count(p => PermitStatusHelper.IsRejectedStatus(p.Status))
                })
                .OrderBy(x => x.year)
                .ThenBy(x => x.month)
                .ToList();

            return monthlyData.Cast<object>().ToList();
        }

        #endregion
    }
    #endregion
}