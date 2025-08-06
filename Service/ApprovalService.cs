using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfGeneratorService _pdfGenerator;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ApprovalService> _logger;

        // Business rules constants
        private const int MINIMUM_REQUIRED_DOCUMENTS = 7;
        private const int PERMIT_VALIDITY_MONTHS = 6;

        public ApprovalService(
            ApplicationDbContext context,
            IPdfGeneratorService pdfGenerator,
            INotificationService notificationService,
            ILogger<ApprovalService> logger)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ApprovalResult> ProcessApprovalAsync(int permitId, string action, string comments, int userId, string userRole)
        {
            _logger.LogInformation("Processing {Action} for permit {PermitId} by user {UserId} ({UserRole})",
                action, permitId, userId, userRole);

            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return new ApprovalResult
                    {
                        Success = false,
                        ErrorMessage = "Permohonan tidak ditemukan"
                    };
                }

                // Validate authorization
                if (!CanUserApprove(userRole, permit.Status))
                {
                    return new ApprovalResult
                    {
                        Success = false,
                        ErrorMessage = "Permohonan tidak dapat diproses pada tahap ini"
                    };
                }

                // Validate before approval
                var validationResult = await ValidateBeforeApprovalAsync(permitId, userRole);
                if (!validationResult.IsValid)
                {
                    return new ApprovalResult
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage
                    };
                }

                // Process the approval/rejection
                var approvalDetails = await ProcessSingleApprovalAsync(permit, action, comments, userId, userRole);

                if (!approvalDetails.Success)
                {
                    return approvalDetails;
                }

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed {Action} for permit {PermitId} - new status: {NewStatus}",
                    action, permitId, permit.Status);

                return new ApprovalResult
                {
                    Success = true,
                    ActionText = approvalDetails.ActionText,
                    NewStatus = permit.Status,
                    PermitApplicationNumber = permit.ApplicationNumber,
                    ProcessingTime = DateTime.Now - permit.SubmissionDate,
                    GeneratedDocumentPath = permit.GeneratedDocumentPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval for permit {PermitId}", permitId);
                return new ApprovalResult
                {
                    Success = false,
                    ErrorMessage = $"Terjadi kesalahan saat memproses approval: {ex.Message}"
                };
            }
        }

        public async Task<BulkApprovalResult> ProcessBulkApprovalAsync(List<int> permitIds, string action, string comments, int userId, string userRole)
        {
            _logger.LogInformation("Processing bulk {Action} for {Count} permits by user {UserId} ({UserRole})",
                action, permitIds.Count, userId, userRole);

            var result = new BulkApprovalResult();

            try
            {
                var permits = await _context.PermitApplications
                    .Where(p => permitIds.Contains(p.Id))
                    .Include(p => p.Documents)
                    .ToListAsync();

                foreach (var permit in permits)
                {
                    try
                    {
                        if (CanUserApprove(userRole, permit.Status))
                        {
                            var approvalResult = await ProcessSingleApprovalAsync(permit, action, comments, userId, userRole);

                            if (approvalResult.Success)
                            {
                                result.SuccessfulPermits.Add(permit.ApplicationNumber);
                                result.SuccessCount++;
                            }
                            else
                            {
                                result.FailedPermits.Add(new BulkFailure
                                {
                                    PermitNumber = permit.ApplicationNumber,
                                    ErrorMessage = approvalResult.ErrorMessage
                                });
                            }
                        }
                        else
                        {
                            result.FailedPermits.Add(new BulkFailure
                            {
                                PermitNumber = permit.ApplicationNumber,
                                ErrorMessage = "Tidak dapat diproses pada tahap ini"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing permit {PermitId} in bulk operation", permit.Id);
                        result.FailedPermits.Add(new BulkFailure
                        {
                            PermitNumber = permit.ApplicationNumber,
                            ErrorMessage = ex.Message
                        });
                    }
                }

                await _context.SaveChangesAsync();

                result.Success = result.SuccessCount > 0;
                result.Message = $"{result.SuccessCount} permohonan berhasil {action.ToLower()}";

                _logger.LogInformation("Bulk approval completed: {SuccessCount} successful, {FailCount} failed",
                    result.SuccessCount, result.FailedPermits.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk approval operation");
                result.Success = false;
                result.Message = $"Terjadi kesalahan dalam operasi bulk: {ex.Message}";
                return result;
            }
        }

        public async Task<ApprovalWithEditResult> ProcessApprovalWithEditsAsync(int permitId, PermitApprovalViewModel model, int userId)
        {
            _logger.LogInformation("Processing approval with edits for permit {PermitId} by admin {UserId}", permitId, userId);

            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .Include(p => p.Documents)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return new ApprovalWithEditResult
                    {
                        Success = false,
                        ErrorMessage = "Permohonan tidak ditemukan"
                    };
                }

                var changedFields = new List<string>();
                var originalData = new Dictionary<string, string>();

                // Apply edits if data was changed
                if (model.IsEditingData && model.Action == "Approve")
                {
                    changedFields.AddRange(await ApplyBasicFieldChanges(permit, model, originalData));
                    changedFields.AddRange(await ApplyLivestockChanges(permit, model));
                }

                // Process the approval
                var approvalResult = await ProcessSingleApprovalAsync(permit, model.Action, model.Comments, userId, "Admin");

                if (!approvalResult.Success)
                {
                    return new ApprovalWithEditResult
                    {
                        Success = false,
                        ErrorMessage = approvalResult.ErrorMessage
                    };
                }

                // Create enhanced comments with change tracking
                var enhancedComments = model.Comments ?? "";
                if (changedFields.Any())
                {
                    enhancedComments += $"\n\n📝 PERUBAHAN DATA OLEH ADMIN:\n{string.Join("\n", changedFields)}";

                    // Update the approval history with enhanced comments
                    var latestHistory = await _context.PermitApprovalHistories
                        .Where(h => h.PermitApplicationId == permitId)
                        .OrderByDescending(h => h.ActionDate)
                        .FirstOrDefaultAsync();

                    if (latestHistory != null)
                    {
                        latestHistory.Comments = enhancedComments;
                    }
                }

                await _context.SaveChangesAsync();

                return new ApprovalWithEditResult
                {
                    Success = true,
                    ActionText = approvalResult.ActionText,
                    NewStatus = permit.Status,
                    PermitApplicationNumber = permit.ApplicationNumber,
                    ChangedFields = changedFields,
                    OriginalData = originalData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval with edits for permit {PermitId}", permitId);
                return new ApprovalWithEditResult
                {
                    Success = false,
                    ErrorMessage = $"Terjadi kesalahan: {ex.Message}"
                };
            }
        }

        public bool CanUserApprove(string userRole, PermitStatus permitStatus)
        {
            return userRole switch
            {
                "Admin" => permitStatus == PermitStatus.Submitted || permitStatus == PermitStatus.UnderAdminReview,
                "Verifikator" => permitStatus == PermitStatus.AdminApproved || permitStatus == PermitStatus.UnderVerifikatorReview,
                "KepalaDinas" => permitStatus == PermitStatus.VerifikatorApproved || permitStatus == PermitStatus.PendingKepalaDinas,
                _ => false
            };
        }

        public async Task<List<LivestockPermitApplication>> GetPendingApprovalsAsync(string userRole, int? userId = null)
        {
            var query = _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.Documents)
                .AsQueryable();

            query = userRole switch
            {
                "User" when userId.HasValue => query.Where(p => p.UserId == userId.Value),
                "Admin" => query.Where(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderAdminReview),
                "Verifikator" => query.Where(p => p.Status == PermitStatus.AdminApproved || p.Status == PermitStatus.UnderVerifikatorReview),
                "KepalaDinas" => query.Where(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas),
                _ => query.Where(p => false) // No access
            };

            return await query
                .OrderByDescending(p => p.SubmissionDate)
                .ToListAsync();
        }

        public async Task<List<PermitApprovalHistory>> GetApprovalHistoryAsync(int permitId)
        {
            return await _context.PermitApprovalHistories
                .Where(h => h.PermitApplicationId == permitId)
                .Include(h => h.User)
                .OrderBy(h => h.ActionDate)
                .ToListAsync();
        }

        public async Task<ApprovalStatistics> GetApprovalStatisticsAsync(int userId, string userRole)
        {
            var stats = new ApprovalStatistics
            {
                UserId = userId,
                UserRole = userRole,
                PeriodStart = DateTime.Now.AddMonths(-1),
                PeriodEnd = DateTime.Now
            };

            try
            {
                if (userRole == "Admin")
                {
                    stats.TotalReviewed = await _context.PermitApplications.CountAsync(p => p.AdminId == userId);
                    stats.TotalApproved = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == userId &&
                        (p.Status == PermitStatus.AdminApproved ||
                         p.Status == PermitStatus.VerifikatorApproved ||
                         p.Status == PermitStatus.FinalApproved));
                    stats.TotalRejected = await _context.PermitApplications.CountAsync(p =>
                        p.AdminId == userId && p.Status == PermitStatus.AdminRejected);
                }
                else if (userRole == "Verifikator")
                {
                    stats.TotalReviewed = await _context.PermitApplications.CountAsync(p => p.VerifikatorId == userId);
                    stats.TotalApproved = await _context.PermitApplications.CountAsync(p =>
                        p.VerifikatorId == userId &&
                        (p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.FinalApproved));
                    stats.TotalRejected = await _context.PermitApplications.CountAsync(p =>
                        p.VerifikatorId == userId && p.Status == PermitStatus.VerifikatorRejected);
                }
                else if (userRole == "KepalaDinas")
                {
                    stats.TotalReviewed = await _context.PermitApplications.CountAsync(p => p.KepalaDinasId == userId);
                    stats.TotalApproved = await _context.PermitApplications.CountAsync(p =>
                        p.KepalaDinasId == userId && p.Status == PermitStatus.FinalApproved);
                    stats.TotalRejected = await _context.PermitApplications.CountAsync(p =>
                        p.KepalaDinasId == userId && p.Status == PermitStatus.KepalaDinasRejected);
                }

                // Calculate average processing time
                if (stats.TotalApproved > 0)
                {
                    var completedPermits = await GetCompletedPermitsByUser(userId, userRole);
                    if (completedPermits.Any())
                    {
                        var totalDays = completedPermits.Sum(p => CalculateProcessingDays(p, userRole));
                        stats.AverageProcessingDays = totalDays / completedPermits.Count;
                    }
                }

                // This month statistics
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                stats.ThisMonthReviewed = await _context.PermitApprovalHistories
                    .CountAsync(h => h.UserId == userId &&
                               h.ActionDate.Month == currentMonth &&
                               h.ActionDate.Year == currentYear);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating approval statistics for user {UserId}", userId);
                return stats; // Return empty stats
            }
        }

        public async Task<List<ApprovalTimelineItem>> GetApprovalTimelineAsync(int permitId)
        {
            var histories = await GetApprovalHistoryAsync(permitId);
            var timeline = new List<ApprovalTimelineItem>();

            foreach (var history in histories)
            {
                timeline.Add(new ApprovalTimelineItem
                {
                    Date = history.ActionDate,
                    Action = history.Action,
                    Actor = history.User.NamaLengkap,
                    ActorRole = history.User.Role,
                    Comments = history.Comments,
                    FromStatus = history.FromStatus,
                    ToStatus = history.ToStatus,
                    IsApproval = history.Action.Contains("Disetujui"),
                    IsRejection = history.Action.Contains("Ditolak")
                });
            }

            return timeline.OrderBy(t => t.Date).ToList();
        }

        public async Task<ApprovalValidationResult> ValidateBeforeApprovalAsync(int permitId, string userRole)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.Documents)
                    .Include(p => p.LivestockDetails)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    return new ApprovalValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Permohonan tidak ditemukan"
                    };
                }

                var validationErrors = new List<string>();

                // Validate documents for admin approval
                if (userRole == "Admin")
                {
                    if (permit.Documents.Count < MINIMUM_REQUIRED_DOCUMENTS)
                    {
                        validationErrors.Add($"Dokumen pendukung belum lengkap. Dibutuhkan minimal {MINIMUM_REQUIRED_DOCUMENTS} dokumen, saat ini: {permit.Documents.Count}");
                    }

                    // Validate livestock details
                    if (!permit.LivestockDetails.Any())
                    {
                        validationErrors.Add("Detail ternak belum diisi");
                    }

                    // Validate basic information
                    if (string.IsNullOrEmpty(permit.CompanyName))
                    {
                        validationErrors.Add("Nama perusahaan/pemohon belum diisi");
                    }

                    if (string.IsNullOrEmpty(permit.OriginLocation) || string.IsNullOrEmpty(permit.DestinationLocation))
                    {
                        validationErrors.Add("Lokasi asal dan tujuan harus diisi");
                    }
                }

                // Additional validation for other roles can be added here

                return new ApprovalValidationResult
                {
                    IsValid = !validationErrors.Any(),
                    ErrorMessage = validationErrors.Any() ? string.Join("; ", validationErrors) : string.Empty,
                    ValidationDetails = validationErrors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating permit {PermitId} before approval", permitId);
                return new ApprovalValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Terjadi kesalahan saat validasi"
                };
            }
        }

        public async Task<AutoAdvanceResult> AutoAdvancePermitAsync(int permitId)
        {
            // This method can be used for automatic status progression
            // based on business rules (e.g., auto-approve if all conditions met)

            var permit = await _context.PermitApplications.FindAsync(permitId);
            if (permit == null)
            {
                return new AutoAdvanceResult
                {
                    Success = false,
                    ErrorMessage = "Permit not found"
                };
            }

            // Implement auto-advance logic based on business rules
            // For now, return not applicable
            return new AutoAdvanceResult
            {
                Success = false,
                ErrorMessage = "Auto-advance not applicable for this permit"
            };
        }

        public async Task SendNewPermitNotificationsAsync(int permitId)
        {
            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == permitId);

                if (permit == null)
                {
                    _logger.LogWarning("Permit {PermitId} not found for notification", permitId);
                    return;
                }

                // Send notification to Admin role
                await _notificationService.SendNewPermitNotificationAsync(permit, "Admin");

                _logger.LogInformation("New permit notifications sent for permit {ApplicationNumber}", permit.ApplicationNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new permit notifications for permit {PermitId}", permitId);
            }
        }

        #region Private Helper Methods

        private async Task<ApprovalResult> ProcessSingleApprovalAsync(LivestockPermitApplication permit, string action, string comments, int userId, string userRole)
        {
            try
            {
                var fromStatus = permit.Status;
                PermitStatus toStatus;
                string actionText;

                if (action == "Approve")
                {
                    var approvalMapping = GetApprovalMapping(userRole);
                    toStatus = approvalMapping.ToStatus;
                    actionText = approvalMapping.ActionText;

                    // Update permit based on role
                    await UpdatePermitForApproval(permit, userRole, userId);

                    // Generate PDF document after admin approval
                    if (userRole == "Admin")
                    {
                        await GeneratePermitDocument(permit);
                    }
                    else if (userRole == "KepalaDinas")
                    {
                        await GeneratePermitDocumentWithSignature(permit);
                    }

                    // Send approval notification
                    await _notificationService.SendApprovalNotificationAsync(permit, action, comments, userRole);

                    // Send new permit notification to next level
                    if (userRole == "Admin")
                    {
                        await _notificationService.SendNewPermitNotificationAsync(permit, "Verifikator");
                    }
                    else if (userRole == "Verifikator")
                    {
                        await _notificationService.SendNewPermitNotificationAsync(permit, "KepalaDinas");
                    }
                    else if (userRole == "KepalaDinas")
                    {
                        // Send final approval notification to user
                        await _notificationService.SendFinalApprovalNotificationAsync(permit, comments);
                    }
                }
                else // Reject
                {
                    var rejectionMapping = GetRejectionMapping(userRole);
                    toStatus = rejectionMapping.ToStatus;
                    actionText = rejectionMapping.ActionText;

                    // Reset approval data for rejection
                    await ResetApprovalDataForRejection(permit, userRole);

                    // Send rejection notification to user
                    await _notificationService.SendRejectionNotificationAsync(permit, comments, userRole);

                    // Send rejection notification to previous level (if applicable)
                    if (userRole == "Verifikator" || userRole == "KepalaDinas")
                    {
                        await _notificationService.SendRejectionToPreviousLevelAsync(permit, comments, userRole);
                    }
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
                    Comments = comments ?? $"{actionText} - {DateTime.Now:yyyy-MM-dd HH:mm}",
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);

                return new ApprovalResult
                {
                    Success = true,
                    ActionText = actionText,
                    NewStatus = toStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessSingleApprovalAsync for permit {PermitId}", permit.Id);
                return new ApprovalResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task ResetApprovalDataForRejection(LivestockPermitApplication permit, string userRole)
        {
            switch (userRole)
            {
                case "Verifikator":
                    // Reset to Admin review state
                    permit.VerifikatorId = null;
                    permit.VerificationDate = null;
                    permit.CurrentApprovalLevel = 2; // Back to Admin review state
                    break;
                case "KepalaDinas":
                    // Reset to Verifikator review state
                    permit.KepalaDinasId = null;
                    permit.FinalApprovalDate = null;
                    permit.ValidFrom = null;
                    permit.ValidUntil = null;
                    permit.CurrentApprovalLevel = 3; // Back to Verifikator review state
                    break;
                case "Admin":
                    // Reset to initial state (User can edit and resubmit)
                    permit.AdminId = null;
                    permit.AdminApprovalDate = null;
                    permit.CurrentApprovalLevel = 1; // Back to initial state
                    break;
            }
        }

        private async Task UpdatePermitForApproval(LivestockPermitApplication permit, string userRole, int userId)
        {
            switch (userRole)
            {
                case "Admin":
                    permit.AdminId = userId;
                    permit.AdminApprovalDate = DateTime.Now;
                    permit.CurrentApprovalLevel = 2;
                    break;

                case "Verifikator":
                    permit.VerifikatorId = userId;
                    permit.VerificationDate = DateTime.Now;
                    permit.CurrentApprovalLevel = 3;
                    break;

                case "KepalaDinas":
                    permit.KepalaDinasId = userId;
                    permit.FinalApprovalDate = DateTime.Now;
                    permit.ValidFrom = DateTime.Now;
                    permit.ValidUntil = DateTime.Now.AddMonths(PERMIT_VALIDITY_MONTHS);
                    permit.CurrentApprovalLevel = 4;
                    break;
            }
        }

        private (PermitStatus ToStatus, string ActionText) GetApprovalMapping(string userRole)
        {
            return userRole switch
            {
                "Admin" => (PermitStatus.AdminApproved, "Disetujui Admin"),
                "Verifikator" => (PermitStatus.VerifikatorApproved, "Disetujui Verifikator"),
                "KepalaDinas" => (PermitStatus.FinalApproved, "Disetujui Kepala Dinas"),
                _ => throw new ArgumentException($"Invalid user role for approval: {userRole}")
            };
        }

        private (PermitStatus ToStatus, string ActionText) GetRejectionMapping(string userRole)
        {
            return userRole switch
            {
                "Admin" => (PermitStatus.Submitted, "Ditolak Admin - Kembali ke User"),
                "Verifikator" => (PermitStatus.UnderAdminReview, "Ditolak Verifikator - Kembali ke Admin"),
                "KepalaDinas" => (PermitStatus.UnderVerifikatorReview, "Ditolak Kepala Dinas - Kembali ke Verifikator"),
                _ => throw new ArgumentException($"Invalid user role for rejection: {userRole}")
            };
        }

        private async Task GeneratePermitDocument(LivestockPermitApplication permit)
        {
            try
            {
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

                _logger.LogInformation("Generated permit document for {ApplicationNumber}: {FilePath}", permit.ApplicationNumber, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating permit document for {ApplicationNumber}", permit.ApplicationNumber);
                permit.GeneratedDocumentPath = $"/documents/permits/error_{permit.Id}_{DateTime.Now:yyyyMMddHHmmss}.html";
            }
        }

        private async Task GeneratePermitDocumentWithSignature(LivestockPermitApplication permit)
        {
            try
            {
                var htmlBytes = await _pdfGenerator.GeneratePermitPdfWithSignature(permit);

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "permits");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"permit_signed_{permit.ApplicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.html";
                var filePath = Path.Combine(uploadsPath, fileName);

                await File.WriteAllBytesAsync(filePath, htmlBytes);
                permit.GeneratedDocumentPath = $"/documents/permits/{fileName}";

                _logger.LogInformation("Generated signed permit document for {ApplicationNumber}: {FilePath}", permit.ApplicationNumber, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signed permit document for {ApplicationNumber}", permit.ApplicationNumber);
                permit.GeneratedDocumentPath = $"/documents/permits/error_{permit.Id}_{DateTime.Now:yyyyMMddHHmmss}.html";
            }
        }

        private async Task<List<string>> ApplyBasicFieldChanges(LivestockPermitApplication permit, PermitApprovalViewModel model, Dictionary<string, string> originalData)
        {
            var changedFields = new List<string>();

            // Track original values
            originalData["CompanyName"] = permit.CompanyName;
            originalData["CompanyAddress"] = permit.CompanyAddress ?? "";
            originalData["OriginLocation"] = permit.OriginLocation ?? "";
            originalData["DestinationLocation"] = permit.DestinationLocation ?? "";

            // Apply changes
            if (!string.IsNullOrEmpty(model.EditableCompanyName) && permit.CompanyName != model.EditableCompanyName.Trim())
            {
                permit.CompanyName = model.EditableCompanyName.Trim();
                changedFields.Add($"Nama Perusahaan: '{originalData["CompanyName"]}' → '{permit.CompanyName}'");
            }

            if (!string.IsNullOrEmpty(model.EditableOriginLocation) && permit.OriginLocation != model.EditableOriginLocation.Trim())
            {
                permit.OriginLocation = model.EditableOriginLocation.Trim();
                changedFields.Add($"Lokasi Asal: '{originalData["OriginLocation"]}' → '{permit.OriginLocation}'");
            }

            if (!string.IsNullOrEmpty(model.EditableDestinationLocation) && permit.DestinationLocation != model.EditableDestinationLocation.Trim())
            {
                permit.DestinationLocation = model.EditableDestinationLocation.Trim();
                changedFields.Add($"Lokasi Tujuan: '{originalData["DestinationLocation"]}' → '{permit.DestinationLocation}'");
            }

            return changedFields;
        }

        private async Task<List<string>> ApplyLivestockChanges(LivestockPermitApplication permit, PermitApprovalViewModel model)
        {
            var changedFields = new List<string>();

            if (model.EditableLivestockDetails == null || !model.EditableLivestockDetails.Any())
            {
                return changedFields;
            }

            // Get current livestock details
            var currentLivestock = permit.LivestockDetails.ToList();
            var originalCount = currentLivestock.Count;
            var originalSummary = string.Join(", ", currentLivestock.Select(l => $"{l.LivestockType}: {l.Quantity} ekor"));

            // Remove existing livestock details to replace with edited ones
            _context.LivestockDetails.RemoveRange(currentLivestock);

            // Add updated livestock details
            var validLivestockDetails = model.EditableLivestockDetails
                .Where(d => !d.IsMarkedForDeletion &&
                           !string.IsNullOrEmpty(d.LivestockType) &&
                           d.Quantity > 0)
                .ToList();

            foreach (var editableLivestock in validLivestockDetails)
            {
                permit.LivestockDetails.Add(new LivestockDetail
                {
                    LivestockType = editableLivestock.LivestockType.Trim(),
                    Quantity = editableLivestock.Quantity,
                    Description = editableLivestock.Description?.Trim()
                });
            }

            // Track changes
            var newCount = validLivestockDetails.Count;
            var newSummary = string.Join(", ", validLivestockDetails.Select(l => $"{l.LivestockType}: {l.Quantity} ekor"));
            var totalOriginal = currentLivestock.Sum(l => l.Quantity);
            var totalNew = validLivestockDetails.Sum(l => l.Quantity);

            if (newCount != originalCount || newSummary != originalSummary)
            {
                changedFields.Add($"Detail Ternak: '{originalSummary}' → '{newSummary}'");
                changedFields.Add($"Total Ternak: {totalOriginal} ekor → {totalNew} ekor");
            }

            return changedFields;
        }

        private async Task<List<LivestockPermitApplication>> GetCompletedPermitsByUser(int userId, string userRole)
        {
            var query = _context.PermitApplications.AsQueryable();

            return userRole switch
            {
                "Admin" => await query.Where(p => p.AdminId == userId && p.AdminApprovalDate.HasValue).ToListAsync(),
                "Verifikator" => await query.Where(p => p.VerifikatorId == userId && p.VerificationDate.HasValue).ToListAsync(),
                "KepalaDinas" => await query.Where(p => p.KepalaDinasId == userId && p.FinalApprovalDate.HasValue).ToListAsync(),
                _ => new List<LivestockPermitApplication>()
            };
        }

        private double CalculateProcessingDays(LivestockPermitApplication permit, string userRole)
        {
            return userRole switch
            {
                "Admin" when permit.AdminApprovalDate.HasValue => (permit.AdminApprovalDate.Value - permit.SubmissionDate).TotalDays,
                "Verifikator" when permit.VerificationDate.HasValue && permit.AdminApprovalDate.HasValue =>
                    (permit.VerificationDate.Value - permit.AdminApprovalDate.Value).TotalDays,
                "KepalaDinas" when permit.FinalApprovalDate.HasValue && permit.VerificationDate.HasValue =>
                    (permit.FinalApprovalDate.Value - permit.VerificationDate.Value).TotalDays,
                _ => 0
            };
        }

        #endregion
    }
}