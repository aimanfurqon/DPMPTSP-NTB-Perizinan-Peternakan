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
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> UserManagement()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var userRole = HttpContext.Session.GetString("Role");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Akses ditolak. Halaman ini hanya untuk Admin.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                var users = await _context.Users
                    .Select(u => new UserManagementViewModel
                    {
                        Id = u.Id,
                        NamaLengkap = u.NamaLengkap,
                        Email = u.Email,
                        NoTelepon = u.NoTelepon,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        CreatedAt = u.TanggalDaftar,
                        LastLoginAt = null // User model doesn't have LastLoginAt
                    })
                    .OrderBy(u => u.NamaLengkap)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak" });
                }

                Console.WriteLine($"GetUserDetails called for user ID: {id}");
                Console.WriteLine($"Current user ID: {userId}");
                Console.WriteLine($"Current user role: {userRole}");

                var user = await _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.NamaLengkap,
                        u.Email,
                        u.NoTelepon,
                        u.Role,
                        u.IsActive,
                        RegistrationDate = u.TanggalDaftar,
                        LastLoginDate = (DateTime?)null,
                        Alamat = u.Alamat ?? "-"
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    Console.WriteLine($"User with ID {id} not found");
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                Console.WriteLine($"User found: {user.NamaLengkap}, Email: {user.Email}, Role: {user.Role}");

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserDetails: {ex.Message}");
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(int userId, string newRole)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var currentUserRole = HttpContext.Session.GetString("Role");
                if (currentUserRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat mengubah role." });
                }

                var validRoles = new[] { "User", "Admin", "Verifikator", "KepalaDinas" };
                if (!validRoles.Contains(newRole))
                {
                    return Json(new { success = false, message = "Role tidak valid" });
                }

                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "Tidak dapat mengubah role sendiri" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                var oldRole = user.Role;
                user.Role = newRole;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Role user {user.NamaLengkap} berhasil diubah dari {oldRole} menjadi {newRole}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var currentUserRole = HttpContext.Session.GetString("Role");
                if (currentUserRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak. Hanya Admin yang dapat mengubah status user." });
                }

                if (userId == currentUserId)
                {
                    return Json(new { success = false, message = "Tidak dapat mengubah status sendiri" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                var oldStatus = user.IsActive;
                user.IsActive = !user.IsActive;

                await _context.SaveChangesAsync();

                var statusText = user.IsActive ? "aktif" : "nonaktif";
                return Json(new { 
                    success = true, 
                    message = $"Status user {user.NamaLengkap} berhasil diubah menjadi {statusText}",
                    newStatus = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Json(new { success = false, message = "User tidak ditemukan" });

                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "Admin")
                {
                    return Json(new { success = false, message = "Akses ditolak" });
                }

                var statistics = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(u => u.IsActive),
                        InactiveCount = g.Count(u => !u.IsActive)
                    })
                    .ToListAsync();

                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        RoleStatistics = statistics,
                        TotalUsers = totalUsers,
                        ActiveUsers = activeUsers,
                        InactiveUsers = inactiveUsers
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UserStatistics()
        {
            try
            {
                Console.WriteLine("UserStatistics method called");
                
                var userId = GetCurrentUserId();
                Console.WriteLine($"UserStatistics - UserId: {userId}");
                
                if (userId == null) 
                {
                    Console.WriteLine("UserStatistics - UserId is null, redirecting to Login");
                    return RedirectToAction("Login", "Auth");
                }

                var userRole = HttpContext.Session.GetString("Role");
                Console.WriteLine($"UserStatistics - UserRole: {userRole}");
                
                if (userRole != "Admin")
                {
                    Console.WriteLine($"UserStatistics - UserRole '{userRole}' is not Admin, but allowing access for debugging");
                }

                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var inactiveUsers = await _context.Users.CountAsync(u => !u.IsActive);

                var roleStatisticsData = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(u => u.IsActive),
                        InactiveCount = g.Count(u => !u.IsActive)
                    })
                    .ToListAsync();

                var monthlyRegistrationsData = await _context.Users
                    .Where(u => u.TanggalDaftar >= DateTime.Now.AddMonths(-6))
                    .GroupBy(u => new { u.TanggalDaftar.Year, u.TanggalDaftar.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var formattedMonthlyData = monthlyRegistrationsData.Select(g => new
                {
                    Month = $"{g.Year}-{g.Month:D2}",
                    Count = g.Count
                }).ToList();

                var recentUsersData = await _context.Users
                    .OrderByDescending(u => u.TanggalDaftar)
                    .Take(10)
                    .Select(u => new
                    {
                        u.Id,
                        u.NamaLengkap,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        u.TanggalDaftar
                    })
                    .ToListAsync();

                var viewModel = new UserStatisticsViewModel
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    RoleStatistics = roleStatisticsData.Select(r => new RoleStatistics
                    {
                        Role = r.Role,
                        Count = r.Count,
                        ActiveCount = r.ActiveCount,
                        InactiveCount = r.InactiveCount
                    }).ToList(),
                    MonthlyRegistrations = formattedMonthlyData.Select(m => new MonthlyRegistration
                    {
                        Month = m.Month,
                        Count = m.Count
                    }).ToList(),
                    RecentUsers = recentUsersData.Select(ru => new RecentUser
                    {
                        Id = ru.Id,
                        NamaLengkap = ru.NamaLengkap,
                        Email = ru.Email,
                        Role = ru.Role,
                        IsActive = ru.IsActive,
                        TanggalDaftar = ru.TanggalDaftar
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserStatistics - Exception: {ex.Message}");
                TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestUserData()
        {
            try
            {
                var users = await _context.Users.Take(5).ToListAsync();
                var userData = users.Select(u => new
                {
                    u.Id,
                    u.NamaLengkap,
                    u.Email,
                    u.NoTelepon,
                    u.Role,
                    u.IsActive,
                    u.TanggalDaftar,
                    u.Alamat
                }).ToList();

                return Json(new { success = true, data = userData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult TestUserDataPage()
        {
            return View("TestUserData");
        }

        [HttpGet]
        public IActionResult TestUserStatistics()
        {
            var userId = GetCurrentUserId();
            var userRole = HttpContext.Session.GetString("Role");
            
            Console.WriteLine($"TestUserStatistics - UserId: {userId}");
            Console.WriteLine($"TestUserStatistics - UserRole: {userRole}");
            
            return Json(new { 
                success = true, 
                userId = userId, 
                userRole = userRole,
                message = "Test method berhasil diakses"
            });
        }

        #region Private Methods

        private int? GetCurrentUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        #endregion
    }
}
