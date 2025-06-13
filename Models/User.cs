using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        public string NamaLengkap { get; set; }

        [Required(ErrorMessage = "Nomor telepon harus diisi")]
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        public string NoTelepon { get; set; }

        public string? Alamat { get; set; }

        public DateTime TanggalDaftar { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Role: "User", "Verifikator", "KepalaDinas"
        public string Role { get; set; } = "User";
    }
}