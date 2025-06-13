using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;
using PerizinanPeternakan.Services;

namespace PerizinanPeternakan.Controllers
{
    public class PermitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfGeneratorService _pdfGenerator;

        public PermitController(ApplicationDbContext context, IPdfGeneratorService pdfGenerator)
        {
            _context = context;
            _pdfGenerator = pdfGenerator;
        }

        // GET: Permit/Index - Daftar permohonan untuk user yang login
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            var permits = new List<PermitListViewModel>();

            if (userRole == "User")
            {
                // User hanya melihat permohonan miliknya
                permits = await _context.PermitApplications
                    .Where(p => p.UserId == userId.Value)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = p.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(p.GeneratedDocumentPath),
                        CanView = true,
                        CanApprove = false
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "Verifikator")
            {
                // Verifikator melihat permohonan yang perlu diverifikasi
                permits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.Submitted || p.Status == PermitStatus.UnderReview)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = false,
                        CanView = true,
                        CanApprove = true
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }
            else if (userRole == "KepalaDinas")
            {
                // Kepala Dinas melihat permohonan yang sudah disetujui verifikator
                permits = await _context.PermitApplications
                    .Where(p => p.Status == PermitStatus.VerifikatorApproved || p.Status == PermitStatus.PendingKepalaDinas)
                    .Select(p => new PermitListViewModel
                    {
                        Id = p.Id,
                        ApplicationNumber = p.ApplicationNumber,
                        CompanyName = p.CompanyName,
                        ApplicantName = p.User.NamaLengkap,
                        Status = p.Status,
                        SubmissionDate = p.SubmissionDate,
                        OriginLocation = p.OriginLocation,
                        DestinationLocation = p.DestinationLocation,
                        CanDownload = false,
                        CanView = true,
                        CanApprove = true
                    })
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToListAsync();
            }

            return View(permits);
        }

        // GET: Permit/Create - Form permohonan baru
        [HttpGet]
        public IActionResult Create()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Auth");
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            var model = new PermitApplicationViewModel();
            model.LivestockDetails.Add(new LivestockDetailViewModel()); // Default 1 item
            return View(model);
        }

        // POST: Permit/Create - Simpan permohonan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermitApplicationViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            if (HttpContext.Session.GetString("Role") != "User")
            {
                TempData["ErrorMessage"] = "Hanya user yang dapat membuat permohonan";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var applicationNumber = await GenerateApplicationNumber();

                var permitApplication = new LivestockPermitApplication
                {
                    ApplicationNumber = applicationNumber,
                    UserId = userId.Value,
                    CompanyName = model.CompanyName,
                    CompanyAddress = model.CompanyAddress,
                    OriginLocation = model.OriginLocation,
                    DestinationLocation = model.DestinationLocation,
                    DeparturePort = model.DeparturePort,
                    ArrivalPort = model.ArrivalPort,
                    Status = PermitStatus.Submitted,
                    SubmissionDate = DateTime.Now,
                    CurrentApprovalLevel = 1
                };

                _context.PermitApplications.Add(permitApplication);
                await _context.SaveChangesAsync();

                // Tambahkan detail ternak
                foreach (var detail in model.LivestockDetails.Where(d => !string.IsNullOrEmpty(d.LivestockType)))
                {
                    var livestockDetail = new LivestockDetail
                    {
                        PermitApplicationId = permitApplication.Id,
                        LivestockType = detail.LivestockType,
                        Quantity = detail.Quantity,
                        Description = detail.Description
                    };
                    _context.LivestockDetails.Add(livestockDetail);
                }

                // Tambahkan history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permitApplication.Id,
                    UserId = userId.Value,
                    FromStatus = PermitStatus.Draft,
                    ToStatus = PermitStatus.Submitted,
                    Action = "Submit",
                    Comments = "Permohonan diajukan",
                    ActionDate = DateTime.Now
                };
                _context.PermitApprovalHistories.Add(history);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Permohonan berhasil diajukan dengan nomor: {applicationNumber}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan permohonan. Silakan coba lagi.");
                return View(model);
            }
        }

        // GET: Permit/Detail/5 - Detail permohonan
        public async Task<IActionResult> Detail(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .Include(p => p.ApprovalHistory)
                    .ThenInclude(h => h.User)
                .Include(p => p.Verifikator)
                .Include(p => p.KepalaDinas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            // Check access rights
            if (userRole == "User" && permit.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke permohonan ini";
                return RedirectToAction("Index");
            }

            var model = new PermitDetailViewModel
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
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                DeparturePort = permit.DeparturePort,
                ArrivalPort = permit.ArrivalPort,
                RejectionReason = permit.RejectionReason,
                ValidFrom = permit.ValidFrom,
                ValidUntil = permit.ValidUntil,
                GeneratedDocumentPath = permit.GeneratedDocumentPath,
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
                CanDownload = permit.Status == PermitStatus.FinalApproved && !string.IsNullOrEmpty(permit.GeneratedDocumentPath),
                CanApprove = CanUserApprove(userRole, permit.Status)
            };

            return View(model);
        }

        // GET: Permit/Approve/5 - Form approval
        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melakukan approval";
                return RedirectToAction("Index");
            }

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            if (!CanUserApprove(userRole, permit.Status))
            {
                TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                return RedirectToAction("Detail", new { id });
            }

            var model = new PermitApprovalViewModel
            {
                Id = permit.Id,
                ApplicationNumber = permit.ApplicationNumber,
                CompanyName = permit.CompanyName,
                ApplicantName = permit.User.NamaLengkap,
                CurrentStatus = permit.Status,
                SubmissionDate = permit.SubmissionDate,
                OriginLocation = permit.OriginLocation,
                DestinationLocation = permit.DestinationLocation,
                LivestockDetails = permit.LivestockDetails.Select(d => new LivestockDetailViewModel
                {
                    LivestockType = d.LivestockType,
                    Quantity = d.Quantity,
                    Description = d.Description
                }).ToList()
            };

            return View(model);
        }

        // POST: Permit/Approve - Proses approval
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(PermitApprovalViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole == "User")
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk melakukan approval";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(model.Action) || (model.Action != "Approve" && model.Action != "Reject"))
            {
                ModelState.AddModelError("Action", "Pilih aksi yang akan dilakukan");
                return View(model);
            }

            try
            {
                var permit = await _context.PermitApplications
                    .Include(p => p.User)
                    .Include(p => p.LivestockDetails)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);

                if (permit == null)
                {
                    TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                    return RedirectToAction("Index");
                }

                if (!CanUserApprove(userRole, permit.Status))
                {
                    TempData["ErrorMessage"] = "Permohonan tidak dapat diproses pada tahap ini";
                    return RedirectToAction("Detail", new { id = model.Id });
                }

                var fromStatus = permit.Status;
                PermitStatus toStatus;
                string actionText;

                if (model.Action == "Approve")
                {
                    if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorApproved;
                        actionText = "Disetujui Verifikator";
                        permit.VerifikatorId = userId.Value;
                        permit.VerificationDate = DateTime.Now;
                        permit.CurrentApprovalLevel = 2;
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.FinalApproved;
                        actionText = "Disetujui Kepala Dinas";
                        permit.KepalaDinasId = userId.Value;
                        permit.FinalApprovalDate = DateTime.Now;
                        permit.ValidFrom = DateTime.Now;
                        permit.ValidUntil = DateTime.Now.AddMonths(6); // Valid 6 bulan
                        permit.CurrentApprovalLevel = 3;

                        // Generate PDF document
                        await GeneratePermitDocument(permit);
                    }
                }
                else // Reject
                {
                    if (userRole == "Verifikator")
                    {
                        toStatus = PermitStatus.VerifikatorRejected;
                        actionText = "Ditolak Verifikator";
                    }
                    else // KepalaDinas
                    {
                        toStatus = PermitStatus.KepalaDinasRejected;
                        actionText = "Ditolak Kepala Dinas";
                    }

                    permit.RejectionReason = model.Comments;
                }

                permit.Status = toStatus;

                // Add approval history
                var history = new PermitApprovalHistory
                {
                    PermitApplicationId = permit.Id,
                    UserId = userId.Value,
                    FromStatus = fromStatus,
                    ToStatus = toStatus,
                    Action = actionText,
                    Comments = model.Comments,
                    ActionDate = DateTime.Now
                };

                _context.PermitApprovalHistories.Add(history);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Permohonan {permit.ApplicationNumber} berhasil {actionText.ToLower()}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan saat memproses approval. Silakan coba lagi.");
                return View(model);
            }
        }

        // GET: Permit/Download/5 - Download PDF
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");

            var permit = await _context.PermitApplications
                .Include(p => p.User)
                .Include(p => p.LivestockDetails)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permit == null)
            {
                TempData["ErrorMessage"] = "Permohonan tidak ditemukan";
                return RedirectToAction("Index");
            }

            // Check access rights
            if (userRole == "User" && permit.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses ke dokumen ini";
                return RedirectToAction("Index");
            }

            if (permit.Status != PermitStatus.FinalApproved || string.IsNullOrEmpty(permit.GeneratedDocumentPath))
            {
                TempData["ErrorMessage"] = "Dokumen belum tersedia untuk didownload";
                return RedirectToAction("Detail", new { id });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", permit.GeneratedDocumentPath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File dokumen tidak ditemukan";
                return RedirectToAction("Detail", new { id });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = $"Izin_Pengeluaran_Ternak_{permit.ApplicationNumber}.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private async Task<string> GenerateApplicationNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            var lastNumber = await _context.PermitApplications
                .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                .CountAsync();

            return $"{(lastNumber + 1).ToString().PadLeft(3, '0')}/03-260/DPM&PTSP/{year}";
        }

        private bool CanUserApprove(string userRole, PermitStatus status)
        {
            return userRole switch
            {
                "Verifikator" => status == PermitStatus.Submitted || status == PermitStatus.UnderReview,
                "KepalaDinas" => status == PermitStatus.VerifikatorApproved || status == PermitStatus.PendingKepalaDinas,
                _ => false
            };
        }

        private async Task GeneratePermitDocument(LivestockPermitApplication permit)
        {
            try
            {
                var pdfBytes = await _pdfGenerator.GeneratePermitPdf(permit);

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "permits");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"permit_{permit.ApplicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(uploadsPath, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // Update permit with document path
                permit.GeneratedDocumentPath = $"/documents/permits/{fileName}";
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval process
                // Consider logging to a proper logging system
                Console.WriteLine($"Error generating PDF: {ex.Message}");
            }
        }

        #endregion
    }
}