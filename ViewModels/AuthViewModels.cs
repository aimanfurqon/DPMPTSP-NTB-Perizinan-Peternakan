using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username/Email harus diisi")]
        [Display(Name = "Username atau Email")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Ingat Saya")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Alamat email harus diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [Display(Name = "Alamat Email Terdaftar")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password baru harus diisi.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username hanya boleh menggunakan huruf, angka, dan underscore")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Konfirmasi password harus diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        [Required(ErrorMessage = "Nomor telepon harus diisi")]
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string NoTelepon { get; set; }

        [Display(Name = "Alamat")]
        [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter")]
        public string? Alamat { get; set; }
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru (kosongkan jika tidak ingin mengubah)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        [Required(ErrorMessage = "Nomor telepon harus diisi")]
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string NoTelepon { get; set; }

        [Display(Name = "Alamat")]
        [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter")]
        public string? Alamat { get; set; }
    }
}