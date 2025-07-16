using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;

namespace PerizinanPeternakan.Controllers
{
    public class QuotaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuotaController> _logger;

        public QuotaController(ApplicationDbContext context, ILogger<QuotaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get quota information by livestock type and province - REAL TIME
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuota(string livestockType, string provinceCode, int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.Now.Year;

                var quota = await _context.LivestockQuotas
                    .Where(q => q.LivestockType == livestockType &&
                               q.ProvinceCode == provinceCode &&
                               q.Year == year &&
                               q.IsActive)
                    .FirstOrDefaultAsync();

                if (quota == null)
                {
                    return Json(new QuotaValidationResponse
                    {
                        IsValid = false,
                        Message = $"Kuota untuk {livestockType} di provinsi ini belum ditetapkan untuk tahun {year}",
                        AvailableQuota = 0,
                        TotalQuota = 0,
                        UsedQuota = 0,
                        LivestockType = livestockType
                    });
                }

                // Hitung real-time usage dari permits yang sudah approved
                var currentUsage = await _context.QuotaUsages
                    .Where(qu => qu.LivestockQuotaId == quota.Id &&
                                qu.Status == "Confirmed")
                    .SumAsync(qu => qu.Quantity);

                // Update used quota di database (real-time)
                quota.UsedQuota = currentUsage;
                quota.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var response = new QuotaValidationResponse
                {
                    IsValid = quota.RemainingQuota > 0,
                    Message = quota.RemainingQuota > 0 ?
                        $"Kuota tersedia: {quota.RemainingQuota:N0} ekor" :
                        "Kuota sudah habis untuk jenis ternak ini",
                    AvailableQuota = quota.RemainingQuota,
                    TotalQuota = quota.TotalQuota,
                    UsedQuota = quota.UsedQuota,
                    UsagePercentage = quota.TotalQuota > 0 ? (double)quota.UsedQuota / quota.TotalQuota * 100 : 0,
                    LivestockType = livestockType,
                    ProvinceName = quota.ProvinceName
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota for {LivestockType} in {ProvinceCode}", livestockType, provinceCode);
                return Json(new QuotaValidationResponse
                {
                    IsValid = false,
                    Message = "Terjadi kesalahan saat mengambil data kuota",
                    AvailableQuota = 0
                });
            }
        }

        /// <summary>
        /// Validate if requested quantity is within quota - REAL TIME
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ValidateQuota([FromBody] QuotaValidationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new QuotaValidationResponse
                    {
                        IsValid = false,
                        Message = "Data request tidak valid"
                    });
                }

                var quota = await _context.LivestockQuotas
                    .Where(q => q.LivestockType == request.LivestockType &&
                               q.ProvinceCode == request.ProvinceCode &&
                               q.Year == request.Year &&
                               q.IsActive)
                    .FirstOrDefaultAsync();

                if (quota == null)
                {
                    return Json(new QuotaValidationResponse
                    {
                        IsValid = false,
                        Message = $"Kuota untuk {request.LivestockType} belum ditetapkan",
                        LivestockType = request.LivestockType
                    });
                }

                // Hitung real-time usage
                var currentUsage = await _context.QuotaUsages
                    .Where(qu => qu.LivestockQuotaId == quota.Id &&
                                qu.Status == "Confirmed")
                    .SumAsync(qu => qu.Quantity);

                quota.UsedQuota = currentUsage;
                var remainingQuota = quota.TotalQuota - currentUsage;

                bool isValid = request.RequestedQuantity <= remainingQuota;
                string message;

                if (isValid)
                {
                    message = $"✅ Jumlah valid. Tersisa {remainingQuota - request.RequestedQuantity:N0} ekor setelah permintaan ini";
                }
                else
                {
                    message = $"❌ Jumlah melebihi kuota. Maksimal yang bisa diminta: {remainingQuota:N0} ekor";
                }

                var response = new QuotaValidationResponse
                {
                    IsValid = isValid,
                    Message = message,
                    AvailableQuota = remainingQuota,
                    TotalQuota = quota.TotalQuota,
                    UsedQuota = currentUsage,
                    UsagePercentage = quota.TotalQuota > 0 ? (double)currentUsage / quota.TotalQuota * 100 : 0,
                    LivestockType = request.LivestockType,
                    ProvinceName = quota.ProvinceName
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quota for {LivestockType}", request.LivestockType);
                return Json(new QuotaValidationResponse
                {
                    IsValid = false,
                    Message = "Terjadi kesalahan saat validasi kuota"
                });
            }
        }

        /// <summary>
        /// Get all quotas by province - untuk dropdown informasi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuotasByProvince(string provinceCode, int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.Now.Year;

                var quotas = await _context.LivestockQuotas
                    .Where(q => q.ProvinceCode == provinceCode && q.Year == year && q.IsActive)
                    .Select(q => new LivestockQuotaDto
                    {
                        LivestockType = q.LivestockType,
                        ProvinceName = q.ProvinceName,
                        TotalQuota = q.TotalQuota,
                        UsedQuota = q.UsedQuota,
                        RemainingQuota = q.RemainingQuota,
                        Year = q.Year,
                        UsagePercentage = q.TotalQuota > 0 ? (double)q.UsedQuota / q.TotalQuota * 100 : 0
                    })
                    .OrderBy(q => q.LivestockType)
                    .ToListAsync();

                return Json(new { success = true, data = quotas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotas for province {ProvinceCode}", provinceCode);
                return Json(new { success = false, message = "Error retrieving quotas" });
            }
        }

        /// <summary>
        /// Reserve quota when user submits form - untuk mencegah over-booking
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ReserveQuota([FromBody] QuotaReservationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var quota = await _context.LivestockQuotas
                    .Where(q => q.LivestockType == request.LivestockType &&
                               q.ProvinceCode == request.ProvinceCode &&
                               q.Year == DateTime.Now.Year &&
                               q.IsActive)
                    .FirstOrDefaultAsync();

                if (quota == null)
                {
                    return Json(new { success = false, message = "Kuota tidak ditemukan" });
                }

                // Check current usage
                var currentUsage = await _context.QuotaUsages
                    .Where(qu => qu.LivestockQuotaId == quota.Id &&
                                (qu.Status == "Confirmed" || qu.Status == "Reserved"))
                    .SumAsync(qu => qu.Quantity);

                if (currentUsage + request.Quantity > quota.TotalQuota)
                {
                    return Json(new { success = false, message = "Kuota tidak mencukupi" });
                }

                // Create reservation
                var reservation = new QuotaUsage
                {
                    LivestockQuotaId = quota.Id,
                    PermitApplicationId = request.PermitApplicationId,
                    Quantity = request.Quantity,
                    Status = "Reserved",
                    UsedAt = DateTime.Now,
                    Notes = $"Reserved for permit application {request.PermitApplicationId}"
                };

                _context.QuotaUsages.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Kuota berhasil direservasi" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reserving quota");
                return Json(new { success = false, message = "Error reserving quota" });
            }
        }

        /// <summary>
        /// Get quota statistics - untuk dashboard admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetQuotaStatistics(int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.Now.Year;

                var stats = await _context.LivestockQuotas
                    .Where(q => q.Year == year && q.IsActive)
                    .GroupBy(q => q.LivestockType)
                    .Select(g => new
                    {
                        livestockType = g.Key,
                        totalProvinces = g.Count(),
                        totalQuota = g.Sum(q => q.TotalQuota),
                        totalUsed = g.Sum(q => q.UsedQuota),
                        averageUsage = g.Average(q => q.TotalQuota > 0 ? (double)q.UsedQuota / q.TotalQuota * 100 : 0)
                    })
                    .ToListAsync();

                return Json(new { success = true, data = stats, year = year });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota statistics");
                return Json(new { success = false, message = "Error retrieving statistics" });
            }
        }
    }

    // Request models
    public class QuotaReservationRequest
    {
        public string LivestockType { get; set; } = string.Empty;
        public string ProvinceCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int PermitApplicationId { get; set; }
    }
}