using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.Service;
using PerizinanPeternakan.Services;
using PerizinanPeternakan.ViewModels;
using System.Text;

namespace PerizinanPeternakan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetCurrentUserData")]
        public async Task<IActionResult> GetCurrentUserData()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");

                if (string.IsNullOrEmpty(username))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session tidak valid atau sudah berakhir. Silakan login kembali.",
                        redirectToLogin = true
                    });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .Select(u => new {
                        namaLengkap = u.NamaLengkap,
                        alamat = u.Alamat,
                        email = u.Email,
                        noTelepon = u.NoTelepon,
                        username = u.Username,
                        userId = u.Id
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Data user tidak ditemukan"
                    });
                }

                var response = new
                {
                    success = true,
                    data = new
                    {
                        namaLengkap = user.namaLengkap ?? "",
                        alamat = user.alamat ?? "",
                        email = user.email ?? "",
                        noTelepon = user.noTelepon ?? "",
                        username = user.username ?? "",
                        hasCompleteProfile = !string.IsNullOrEmpty(user.namaLengkap) &&
                                           !string.IsNullOrEmpty(user.alamat),
                        displayName = !string.IsNullOrEmpty(user.namaLengkap) ? user.namaLengkap : user.username,
                        dataFetchedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        profileCompleteness = CalculateProfileCompleteness(user),
                        sessionInfo = new
                        {
                            role = HttpContext.Session.GetString("Role") ?? "",
                            namaLengkapSession = HttpContext.Session.GetString("NamaLengkap") ?? "",
                            isAuthenticated = true
                        }
                    },
                    message = "Data user berhasil dimuat"
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil data user",
                    error = ex.Message
                });
            }
        }

        [HttpGet("GetCurrentUserForIndividualForm")]
        public async Task<IActionResult> GetCurrentUserForIndividualForm()
        {
            try
            {
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User tidak terautentikasi" });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new { success = false, message = "Data user tidak ditemukan" });
                }

                var individualFormData = new
                {
                    success = true,
                    data = new
                    {
                        individualName = user.NamaLengkap ?? "",
                        individualAddress = user.Alamat ?? "",
                        individualProvince = "",
                        individualRegency = "",
                        email = user.Email ?? "",
                        noTelepon = user.NoTelepon ?? "",
                        hasAddress = !string.IsNullOrEmpty(user.Alamat),
                        hasName = !string.IsNullOrEmpty(user.NamaLengkap),
                        needsLocationData = string.IsNullOrEmpty(user.Alamat),
                        suggestions = GenerateFormSuggestions(user)
                    },
                    message = "Data berhasil dimuat untuk form perorangan"
                };

                return Json(individualFormData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCurrentUserForIndividualForm: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil data user"
                });
            }
        }

        [HttpPost("UpdateUserProfileFromForm")]
        public async Task<IActionResult> UpdateUserProfileFromForm([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var username = User.Identity.Name;
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "User tidak terautentikasi" });
                }

                var user = await _context.Users
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                bool hasChanges = false;

                if (!string.IsNullOrEmpty(request.NamaLengkap) && user.NamaLengkap != request.NamaLengkap)
                {
                    user.NamaLengkap = request.NamaLengkap.Trim();
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.Alamat) && user.Alamat != request.Alamat)
                {
                    user.Alamat = request.Alamat.Trim();
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.NoTelepon) && user.NoTelepon != request.NoTelepon)
                {
                    user.NoTelepon = request.NoTelepon.Trim();
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Updated profile for user: {username}");

                    return Json(new
                    {
                        success = true,
                        message = "Profil berhasil diperbarui",
                        data = new
                        {
                            namaLengkap = user.NamaLengkap,
                            alamat = user.Alamat,
                            noTelepon = user.NoTelepon
                        }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = "Tidak ada perubahan data",
                        data = new
                        {
                            namaLengkap = user.NamaLengkap,
                            alamat = user.Alamat,
                            noTelepon = user.NoTelepon
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating user profile: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat memperbarui profil"
                });
            }
        }

        [HttpPost("ValidateIndividualData")]
        public IActionResult ValidateIndividualData([FromBody] IndividualValidationRequest request)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                if (string.IsNullOrWhiteSpace(request.IndividualName))
                    errors.Add("Nama lengkap wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualProvince))
                    errors.Add("Provinsi wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualRegency))
                    errors.Add("Kabupaten/Kota wajib diisi");

                if (string.IsNullOrWhiteSpace(request.IndividualAddress))
                    errors.Add("Alamat lengkap wajib diisi");

                if (!string.IsNullOrEmpty(request.IndividualName))
                {
                    if (request.IndividualName.Length < 3)
                        warnings.Add("Nama terlalu pendek, pastikan nama lengkap sudah benar");

                    if (request.IndividualName.Split(' ').Length < 2)
                        warnings.Add("Disarankan menggunakan nama lengkap (minimal 2 kata)");
                }

                if (!string.IsNullOrEmpty(request.IndividualAddress))
                {
                    if (request.IndividualAddress.Length < 10)
                        warnings.Add("Alamat terlalu pendek, pastikan alamat lengkap sudah benar");
                }

                return Json(new
                {
                    success = true,
                    isValid = errors.Count == 0,
                    errors = errors,
                    warnings = warnings,
                    validationSummary = new
                    {
                        totalErrors = errors.Count,
                        totalWarnings = warnings.Count,
                        canProceed = errors.Count == 0,
                        severity = errors.Count > 0 ? "error" : (warnings.Count > 0 ? "warning" : "success")
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in individual validation: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat validasi data"
                });
            }
        }

        [HttpGet("GetLocationSuggestions")]
        public async Task<IActionResult> GetLocationSuggestions(string query, string type = "province")
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var suggestions = new List<object>();

                if (type.ToLower() == "province")
                {
                    var provinces = new[]
                    {
                        "Nusa Tenggara Barat",
                        "Nusa Tenggara Timur",
                        "Bali",
                        "Jawa Barat",
                        "Jawa Tengah",
                        "Jawa Timur",
                        "DKI Jakarta"
                    };

                    suggestions = provinces
                        .Where(p => p.ToLower().Contains(query.ToLower()))
                        .Select(p => new { text = p, value = p })
                        .ToList<object>();
                }
                else if (type.ToLower() == "regency")
                {
                    var regencies = new[]
                    {
                        "Kota Mataram",
                        "Kabupaten Lombok Barat",
                        "Kabupaten Lombok Tengah",
                        "Kabupaten Lombok Timur",
                        "Kabupaten Lombok Utara",
                        "Kabupaten Sumbawa",
                        "Kabupaten Sumbawa Barat",
                        "Kabupaten Dompu",
                        "Kabupaten Bima",
                        "Kota Bima"
                    };

                    suggestions = regencies
                        .Where(r => r.ToLower().Contains(query.ToLower()))
                        .Select(r => new { text = r, value = r })
                        .ToList<object>();
                }

                return Json(new
                {
                    success = true,
                    data = suggestions,
                    query = query,
                    type = type,
                    count = suggestions.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting location suggestions: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Terjadi kesalahan saat mengambil saran lokasi"
                });
            }
        }

        [HttpGet("GetPortSuggestions")]
        public async Task<IActionResult> GetPortSuggestions(string provinceCode, string term = "")
        {
            try
            {
                var query = _context.Ports
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(provinceCode))
                {
                    query = query.Where(p => p.ProvinceCode == provinceCode);
                }

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(p =>
                        p.Name.Contains(term) ||
                        p.City.Contains(term));
                }

                var ports = await query
                    .OrderBy(p => p.Name)
                    .Take(20)
                    .Select(p => new
                    {
                        id = p.Name,
                        text = $"{p.Name} ({p.City})",
                        city = p.City,
                        province = p.Province,
                        type = p.Type
                    })
                    .ToListAsync();

                return Json(new { results = ports });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting port suggestions: {ex.Message}");
                return Json(new { results = new List<object>() });
            }
        }

        [HttpGet("ParseLocationString")]
        public async Task<IActionResult> ParseLocationString(string locationString)
        {
            try
            {
                if (string.IsNullOrEmpty(locationString))
                {
                    return Json(new { success = false, message = "Location string is empty" });
                }

                var parts = locationString.Split(',');
                if (parts.Length >= 2)
                {
                    var regencyName = parts[0].Trim();
                    var provinceName = parts[1].Trim();

                    var result = new
                    {
                        success = true,
                        provinceCode = GetProvinceCodeByName(provinceName),
                        provinceName = provinceName,
                        regencyCode = GetRegencyCodeByName(regencyName),
                        regencyName = regencyName
                    };

                    return Json(result);
                }

                return Json(new { success = false, message = "Invalid location format" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ValidateEditData")]
        public async Task<IActionResult> ValidateEditData([FromBody] EditValidationRequest request)
        {
            try
            {
                var errors = new List<string>();
                var warnings = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CompanyName))
                    errors.Add("Nama perusahaan harus diisi");

                if (string.IsNullOrWhiteSpace(request.OriginLocation))
                    errors.Add("Lokasi asal harus diisi");

                if (string.IsNullOrWhiteSpace(request.DestinationLocation))
                    errors.Add("Lokasi tujuan harus diisi");

                if (request.LivestockDetails == null || !request.LivestockDetails.Any())
                {
                    errors.Add("Minimal harus ada satu detail ternak");
                }
                else
                {
                    var validLivestock = request.LivestockDetails.Where(l =>
                        !string.IsNullOrEmpty(l.LivestockType) && l.Quantity > 0).ToList();

                    if (!validLivestock.Any())
                    {
                        errors.Add("Minimal harus ada satu detail ternak yang valid");
                    }

                    if (!string.IsNullOrEmpty(request.OriginProvinceCode))
                    {
                        foreach (var livestock in validLivestock)
                        {
                            var quotaValidation = await ValidateQuotaForEdit(
                                livestock.LivestockType,
                                request.OriginProvinceCode,
                                livestock.Quantity);

                            if (!quotaValidation.IsValid)
                            {
                                errors.Add($"{livestock.LivestockType}: {quotaValidation.Message}");
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    isValid = errors.Count == 0,
                    errors = errors,
                    warnings = warnings
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Terjadi kesalahan validasi: {ex.Message}"
                });
            }
        }

        #region Private Methods

        private int CalculateProfileCompleteness(dynamic user)
        {
            try
            {
                int totalFields = 4;
                int completedFields = 0;

                if (!string.IsNullOrEmpty(user.namaLengkap)) completedFields++;
                if (!string.IsNullOrEmpty(user.alamat)) completedFields++;
                if (!string.IsNullOrEmpty(user.email)) completedFields++;
                if (!string.IsNullOrEmpty(user.noTelepon)) completedFields++;

                return (int)Math.Round((double)completedFields / totalFields * 100);
            }
            catch
            {
                return 0;
            }
        }

        private List<string> GenerateFormSuggestions(User user)
        {
            var suggestions = new List<string>();

            if (string.IsNullOrEmpty(user.NamaLengkap))
            {
                suggestions.Add("Lengkapi nama lengkap di profil untuk mempercepat pengisian form");
            }

            if (string.IsNullOrEmpty(user.Alamat))
            {
                suggestions.Add("Tambahkan alamat di profil untuk mempercepat pengisian form");
            }

            if (string.IsNullOrEmpty(user.NoTelepon))
            {
                suggestions.Add("Tambahkan nomor telepon di profil untuk keperluan komunikasi");
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add("Profil Anda sudah lengkap!");
            }

            return suggestions;
        }

        private string GetProvinceCodeByName(string provinceName)
        {
            if (string.IsNullOrEmpty(provinceName))
                return "";

            var provinceMap = new Dictionary<string, string>
            {
                { "Nusa Tenggara Barat", "52" },
                { "Nusa Tenggara Timur", "53" },
                { "Bali", "51" },
                { "Jawa Timur", "35" },
                { "Jawa Tengah", "33" },
                { "Jawa Barat", "32" },
                { "DKI Jakarta", "31" }
            };

            return provinceMap.ContainsKey(provinceName) ? provinceMap[provinceName] : "";
        }

        private string GetRegencyCodeByName(string regencyName)
        {
            if (string.IsNullOrEmpty(regencyName))
                return "";

            var regencyMap = new Dictionary<string, string>
            {
                { "Kota Mataram", "52.71" },
                { "Kabupaten Lombok Barat", "52.01" },
                { "Kabupaten Lombok Tengah", "52.02" },
                { "Kabupaten Lombok Timur", "52.03" },
                { "Kabupaten Lombok Utara", "52.08" },
                { "Kabupaten Sumbawa", "52.04" },
                { "Kabupaten Sumbawa Barat", "52.07" },
                { "Kabupaten Dompu", "52.05" },
                { "Kabupaten Bima", "52.06" },
                { "Kota Bima", "52.72" }
            };

            return regencyMap.ContainsKey(regencyName) ? regencyMap[regencyName] : "";
        }

        private async Task<(bool IsValid, string Message)> ValidateQuotaForEdit(string livestockType, string provinceCode, int quantity)
        {
            try
            {
                return (true, "Kuota tersedia");
            }
            catch (Exception ex)
            {
                return (false, $"Error validating quota: {ex.Message}");
            }
        }

        #endregion

        #region Request Models

        public class UpdateProfileRequest
        {
            public string? NamaLengkap { get; set; }
            public string? Alamat { get; set; }
            public string? NoTelepon { get; set; }
            public string? Email { get; set; }
        }

        public class IndividualValidationRequest
        {
            public string? IndividualName { get; set; }
            public string? IndividualProvince { get; set; }
            public string? IndividualRegency { get; set; }
            public string? IndividualAddress { get; set; }
        }

        public class EditValidationRequest
        {
            public int PermitId { get; set; }
            public string CompanyName { get; set; }
            public string CompanyAddress { get; set; }
            public string OriginLocation { get; set; }
            public string DestinationLocation { get; set; }
            public string DeparturePort { get; set; }
            public string ArrivalPort { get; set; }
            public string OriginProvinceCode { get; set; }
            public List<EditLivestockRequest> LivestockDetails { get; set; } = new();
        }

        public class EditLivestockRequest
        {
            public string LivestockType { get; set; }
            public int Quantity { get; set; }
            public string Description { get; set; }
        }

        #endregion
    }
}
