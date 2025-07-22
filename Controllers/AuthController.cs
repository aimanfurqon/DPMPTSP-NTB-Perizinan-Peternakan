using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;
using PerizinanPeternakan.Services;
using BCrypt.Net;
using System.Security.Cryptography;

namespace PerizinanPeternakan.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AuthController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // POST: Login (Diperbarui dengan Bypass)
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

            if (user == null)
            {
                TempData["ErrorMessage"] = "Username atau Password tidak valid.";
                return View(model);
            }

            // --- PERUBAHAN: BYPASS PASSWORD UNTUK 'kepaladinas' ---
            bool isPasswordValid;

            if (user.Username.Equals("kepaladinas", StringComparison.OrdinalIgnoreCase))
            {
                isPasswordValid = true; // Langsung dianggap valid jika username adalah 'kepaladinas'
                TempData["InfoMessage"] = "Login sebagai Kepala Dinas (Mode Bypass Password Aktif).";
            } else if (user.Username.Equals("admin1", StringComparison.OrdinalIgnoreCase))
            {
                isPasswordValid = true; // Langsung dianggap valid jika username adalah 'kepaladinas'
                TempData["InfoMessage"] = "Login sebagai Admin1 (Mode Bypass Password Aktif).";
            }
            else
            {
                // Verifikasi password normal untuk user lain
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
            }

            if (!isPasswordValid)
            {
                TempData["ErrorMessage"] = "Username atau Password tidak valid.";
                return View(model);
            }
            // --- AKHIR PERUBAHAN ---

            //if (!user.IsEmailVerified)
            //{
            //    TempData["ErrorMessage"] = "Akun Anda belum diverifikasi. Silakan cek email Anda.";
            //    return View(model);
            //}

            SetUserSession(user);
            TempData["SuccessMessage"] = $"Selamat datang kembali, {user.NamaLengkap}!";
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        // ... (Sisa kode Anda: Register, Logout, Profile, dll. tetap sama)
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

            // Cek duplikasi
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

            // Buat token verifikasi yang aman
            var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

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
                IsActive = true, // Tetap aktif, tapi verifikasi email jadi penentu
                IsEmailVerified = false, // <-- PENTING
                VerificationToken = verificationToken,
                VerificationTokenExpires = DateTime.UtcNow.AddDays(1) // Token berlaku 1 hari
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Kirim email verifikasi
            var verificationLink = Url.Action("VerifyEmail", "Auth",
        new { userId = newUser.Id, token = verificationToken },
        Request.Scheme);

            var emailMessage = $"<h1>Verifikasi Akun Anda</h1>" +
                     $"<p>Terima kasih telah mendaftar. Silakan klik link di bawah ini untuk mengaktifkan akun Anda:</p>" +
                     $"<a href='{verificationLink}'>Verifikasi Akun Saya</a>" +
                     $"<p>Jika Anda tidak merasa mendaftar, abaikan email ini.</p>";

            await _emailSender.SendEmailAsync(newUser.Email, "Verifikasi Akun - Sistem Perizinan", emailMessage);

            // Redirect ke halaman informasi
            return RedirectToAction(nameof(RegistrationPending));
        }

        [HttpGet]
        public IActionResult RegistrationPending()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(int userId, string token)
        {
            if (userId == 0 || string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(VerificationFailed));
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.VerificationToken != token || user.VerificationTokenExpires < DateTime.UtcNow)
            {
                return RedirectToAction(nameof(VerificationFailed));
            }

            user.IsEmailVerified = true;
            user.VerificationToken = null; // Hapus token setelah digunakan
            user.VerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            // Opsional: Langsung login setelah verifikasi berhasil
            SetUserSession(user);
            TempData["SuccessMessage"] = "Email berhasil diverifikasi! Anda telah login.";
            return RedirectToAction(nameof(DashboardController.Index), "Dashboard");
        }

        [HttpGet]
        public IActionResult VerificationFailed()
        {
            return View();
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
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
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

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId.Value))
            {
                ModelState.AddModelError(nameof(model.Email), "Email ini sudah terdaftar untuk akun lain.");
                return View(model);
            }

            userToUpdate.Email = model.Email;
            userToUpdate.NamaLengkap = model.NamaLengkap;
            userToUpdate.NoTelepon = model.NoTelepon;
            userToUpdate.Alamat = model.Alamat;

            if (!string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(model.ConfirmPassword);
            }

            _context.Update(userToUpdate);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("NamaLengkap", userToUpdate.NamaLengkap);

            TempData["SuccessMessage"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Profile));
        }

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

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                // Buat token reset
                var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpires = DateTime.UtcNow.AddHours(1); // Token berlaku 1 jam
                await _context.SaveChangesAsync();

                // Kirim email reset
                var resetLink = Url.Action("ResetPassword", "Auth",
                    new { email = user.Email, token = resetToken },
                    Request.Scheme);

                var emailMessage = $"<h1>Reset Password Akun Anda</h1>" +
                                   $"<p>Anda menerima email ini karena ada permintaan untuk mereset password akun Anda. " +
                                   $"Silakan klik link di bawah ini untuk melanjutkan:</p>" +
                                   $"<a href='{resetLink}'>Reset Password Saya</a>" +
                                   $"<p>Link ini akan kedaluwarsa dalam 1 jam. Jika Anda tidak merasa meminta ini, abaikan email ini.</p>";

                await _emailSender.SendEmailAsync(user.Email, "Reset Password - Sistem Perizinan", emailMessage);
            }

            // Selalu tampilkan halaman konfirmasi, bahkan jika email tidak ditemukan.
            // Ini adalah praktik keamanan untuk mencegah orang menebak-nebak email yang terdaftar.
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Auth/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login"); // Atau tampilkan halaman error
            }

            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || user.PasswordResetToken != model.Token || user.ResetTokenExpires < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Link reset password tidak valid atau sudah kedaluwarsa. Silakan coba lagi.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.PasswordResetToken = null; // Hapus token setelah digunakan
            user.ResetTokenExpires = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password Anda telah berhasil direset. Silakan login dengan password baru.";
            return RedirectToAction(nameof(Login));
        }
    }
}
