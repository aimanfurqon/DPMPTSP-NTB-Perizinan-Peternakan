using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.ViewModels
{
    /// <summary>
    /// View model for user login functionality.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Gets or sets the username or email for login.
        /// </summary>
        [Required(ErrorMessage = "Username/Email harus diisi")]
        [Display(Name = "Username atau Email")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password for login.
        /// </summary>
        [Required(ErrorMessage = "Password harus diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to remember the user's login.
        /// </summary>
        [Display(Name = "Ingat Saya")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// View model for forgot password functionality.
    /// </summary>
    public class ForgotPasswordViewModel
    {
        /// <summary>
        /// Gets or sets the registered email address for password reset.
        /// </summary>
        [Required(ErrorMessage = "Alamat email harus diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        [Display(Name = "Alamat Email Terdaftar")]
        public string Email { get; set; }
    }

    /// <summary>
    /// View model for password reset functionality.
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        /// Gets or sets the reset token.
        /// </summary>
        [Required]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [Required]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        [Required(ErrorMessage = "Password baru harus diisi.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password confirmation.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// View model for user registration functionality.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// Gets or sets the username for registration.
        /// </summary>
        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username hanya boleh menggunakan huruf, angka, dan underscore")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email address for registration.
        /// </summary>
        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password for registration.
        /// </summary>
        [Required(ErrorMessage = "Password harus diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password confirmation for registration.
        /// </summary>
        [Required(ErrorMessage = "Konfirmasi password harus diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// Gets or sets the full name for registration.
        /// </summary>
        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        /// <summary>
        /// Gets or sets the phone number for registration.
        /// </summary>
        [Required(ErrorMessage = "Nomor telepon harus diisi")]
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string NoTelepon { get; set; }

        /// <summary>
        /// Gets or sets the address for registration.
        /// </summary>
        [Display(Name = "Alamat")]
        [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter")]
        public string? Alamat { get; set; }
    }

    /// <summary>
    /// View model for user profile management.
    /// </summary>
    public class ProfileViewModel
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Required(ErrorMessage = "Username harus diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [Required(ErrorMessage = "Email harus diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the new password for profile update.
        /// </summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru (kosongkan jika tidak ingin mengubah)")]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the password confirmation for profile update.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        [Required(ErrorMessage = "Nama lengkap harus diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string NamaLengkap { get; set; }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        [Required(ErrorMessage = "Nomor telepon harus diisi")]
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string NoTelepon { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [Display(Name = "Alamat")]
        [StringLength(500, ErrorMessage = "Alamat maksimal 500 karakter")]
        public string? Alamat { get; set; }
    }
}