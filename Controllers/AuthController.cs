using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

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
            // Jika sudah login, redirect ke dashboard
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
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

            try
            {
                // Cari user berdasarkan username atau email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Username == model.Username || u.Email == model.Username)
                                           && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Username/Email atau password salah");
                    return View(model);
                }

                // Verifikasi password
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "Username/Email atau password salah");
                    return View(model);
                }

                // Set session
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("NamaLengkap", user.NamaLengkap);
                HttpContext.Session.SetString("Role", user.Role);

                TempData["SuccessMessage"] = $"Selamat datang, {user.NamaLengkap}!";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan sistem. Silakan coba lagi.");
                return View(model);
            }
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            // Jika sudah login, redirect ke dashboard
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
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

            try
            {
                // Cek apakah username sudah ada
                var existingUsername = await _context.Users
                    .AnyAsync(u => u.Username == model.Username);

                if (existingUsername)
                {
                    ModelState.AddModelError("Username", "Username sudah digunakan");
                    return View(model);
                }

                // Cek apakah email sudah ada
                var existingEmail = await _context.Users
                    .AnyAsync(u => u.Email == model.Email);

                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email sudah terdaftar");
                    return View(model);
                }

                // Buat user baru
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    NamaLengkap = model.NamaLengkap,
                    NoTelepon = model.NoTelepon,
                    Alamat = model.Alamat,
                    Role = "User", // Default role
                    TanggalDaftar = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registrasi berhasil! Silakan login dengan akun Anda.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan sistem. Silakan coba lagi.");
                return View(model);
            }
        }

        // POST: Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Anda telah berhasil logout";
            return RedirectToAction("Login");
        }

        // GET: Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                Username = user.Username,
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
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Update data user
                user.Email = model.Email;
                user.NamaLengkap = model.NamaLengkap;
                user.NoTelepon = model.NoTelepon;
                user.Alamat = model.Alamat;

                // Update password jika diisi
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }

                _context.Update(user);
                await _context.SaveChangesAsync();

                // Update session
                HttpContext.Session.SetString("NamaLengkap", user.NamaLengkap);

                TempData["SuccessMessage"] = "Profile berhasil diperbarui";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Terjadi kesalahan sistem. Silakan coba lagi.");
                return View(model);
            }
        }
    }
}