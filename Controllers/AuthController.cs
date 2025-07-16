using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;
using BCrypt.Net;

namespace PerizinanPeternakan.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            if (IsUserLoggedIn())
            {
                return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
            }
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Username) && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                TempData["ErrorMessage"] = "Username atau Password tidak valid.";
                return View(model);
            }

            SetUserSession(user);
            TempData["SuccessMessage"] = $"Selamat datang kembali, {user.NamaLengkap}!";
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            if (IsUserLoggedIn())
            {
                return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
            }
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError(nameof(model.Username), "Username sudah digunakan.");
                return View(model);
            }
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email sudah terdaftar.");
                return View(model);
            }

            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                NamaLengkap = model.NamaLengkap,
                NoTelepon = model.NoTelepon,
                Alamat = model.Alamat,
                Role = "User",
                TanggalDaftar = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registrasi berhasil! Silakan login dengan akun baru Anda.";
            return RedirectToAction(nameof(Login));
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Anda telah berhasil logout.";
            return RedirectToAction(nameof(Login));
        }

        // --- FUNGSI PROFILE DIKEMBALIKAN ---

        // GET: Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login));
            }

            var model = new ProfileViewModel
            {
                Username = user.Username, // Username tidak bisa diubah, jadi hanya ditampilkan
                Email = user.Email,
                NamaLengkap = user.NamaLengkap,
                NoTelepon = user.NoTelepon,
                Alamat = user.Alamat
            };

            return View(model);
        }

        // POST: Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge(); // Atau RedirectToAction(nameof(Login))
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userToUpdate = await _context.Users.FindAsync(userId.Value);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Cek apakah email baru sudah digunakan oleh user lain
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId.Value))
            {
                ModelState.AddModelError(nameof(model.Email), "Email ini sudah terdaftar untuk akun lain.");
                return View(model);
            }

            // Update data user
            userToUpdate.Email = model.Email;
            userToUpdate.NamaLengkap = model.NamaLengkap;
            userToUpdate.NoTelepon = model.NoTelepon;
            userToUpdate.Alamat = model.Alamat;

            // Update password hanya jika diisi
            if (!string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(model.ConfirmPassword);
            }

            _context.Update(userToUpdate);
            await _context.SaveChangesAsync();

            // Update session jika nama lengkap berubah
            HttpContext.Session.SetString("NamaLengkap", userToUpdate.NamaLengkap);

            TempData["SuccessMessage"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Profile));
        }


        // --- Private Helper Methods ---

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        private void SetUserSession(User user)
        {
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("NamaLengkap", user.NamaLengkap);
            HttpContext.Session.SetString("Role", user.Role);
        }
    }
}