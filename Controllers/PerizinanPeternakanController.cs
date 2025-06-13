using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;

namespace PerizinanPeternakan.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Cek apakah user sudah login
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Ambil informasi user
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            // Data untuk dashboard
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.UserRole = user.Role;
            ViewBag.UserName = user.NamaLengkap;

            return View();
        }

        // Method untuk mengecek otentikasi (bisa dipanggil dari action lain)
        private bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }
    }
}
