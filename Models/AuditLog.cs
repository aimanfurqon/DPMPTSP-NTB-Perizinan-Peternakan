using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PerizinanPeternakan.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(50)]
        public string? SessionId { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public bool Success { get; set; } = true;

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        [StringLength(100)]
        public string? Source { get; set; } = "Web";

        public DateTime? ProcessedAt { get; set; }

        [StringLength(50)]
        public string? ProcessedBy { get; set; }

        public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

        // Additional context data
        [StringLength(1000)]
        public string? AdditionalData { get; set; }

        // Risk assessment
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

        public bool RequiresReview { get; set; } = false;

        public bool IsReviewed { get; set; } = false;

        public DateTime? ReviewedAt { get; set; }

        [StringLength(50)]
        public string? ReviewedBy { get; set; }

        [StringLength(500)]
        public string? ReviewNotes { get; set; }

        // Helper method to create audit log entry
        public static AuditLog Create(
            string action,
            string entityName,
            int entityId,
            string userId,
            string userName,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? sessionId = null,
            string? description = null,
            AuditSeverity severity = AuditSeverity.Information,
            RiskLevel riskLevel = RiskLevel.Low,
            string? source = "Web")
        {
            var auditLog = new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                Description = description,
                Severity = severity,
                RiskLevel = riskLevel,
                Source = source,
                Timestamp = DateTime.Now,
                Success = true
            };

            // Serialize values
            if (oldValues != null)
            {
                auditLog.OldValues = JsonSerializer.Serialize(oldValues, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            if (newValues != null)
            {
                auditLog.NewValues = JsonSerializer.Serialize(newValues, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            // Determine if requires review based on risk level and action
            auditLog.RequiresReview = DetermineRequiresReview(action, riskLevel, severity);

            return auditLog;
        }

        // Create error audit log
        public static AuditLog CreateError(
            string action,
            string entityName,
            int entityId,
            string userId,
            string userName,
            string errorMessage,
            string? ipAddress = null,
            string? userAgent = null,
            string? sessionId = null,
            AuditSeverity severity = AuditSeverity.Error)
        {
            return new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                ErrorMessage = errorMessage,
                Severity = severity,
                RiskLevel = RiskLevel.High,
                Timestamp = DateTime.Now,
                Success = false,
                RequiresReview = true
            };
        }

        // Helper methods
        public T? GetOldValues<T>() where T : class
        {
            if (string.IsNullOrEmpty(OldValues)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(OldValues, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        public T? GetNewValues<T>() where T : class
        {
            if (string.IsNullOrEmpty(NewValues)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(NewValues, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        public void MarkAsReviewed(string reviewedBy, string? notes = null)
        {
            IsReviewed = true;
            ReviewedAt = DateTime.Now;
            ReviewedBy = reviewedBy;
            ReviewNotes = notes;
        }

        public string GetSeverityDisplayName()
        {
            return Severity switch
            {
                AuditSeverity.Trace => "Trace",
                AuditSeverity.Debug => "Debug",
                AuditSeverity.Information => "Informasi",
                AuditSeverity.Warning => "Peringatan",
                AuditSeverity.Error => "Error",
                AuditSeverity.Critical => "Kritis",
                _ => "Unknown"
            };
        }

        public string GetRiskLevelDisplayName()
        {
            return RiskLevel switch
            {
                RiskLevel.Low => "Rendah",
                RiskLevel.Medium => "Sedang",
                RiskLevel.High => "Tinggi",
                RiskLevel.Critical => "Kritis",
                _ => "Unknown"
            };
        }

        public string GetActionDisplayName()
        {
            return Action switch
            {
                AuditActions.Create => "Buat",
                AuditActions.Update => "Ubah",
                AuditActions.Delete => "Hapus",
                AuditActions.Login => "Login",
                AuditActions.Logout => "Logout",
                AuditActions.Approve => "Setujui",
                AuditActions.Reject => "Tolak",
                AuditActions.Submit => "Kirim",
                AuditActions.Download => "Unduh",
                AuditActions.View => "Lihat",
                _ => Action
            };
        }

        public bool IsHighRisk()
        {
            return RiskLevel >= RiskLevel.High ||
                   Severity >= AuditSeverity.Error ||
                   Action.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                   Action.Contains("Admin", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSecurityRelated()
        {
            var securityActions = new[] { "Login", "Logout", "Register", "PasswordChange", "PermissionChange", "RoleChange" };
            return securityActions.Any(a => Action.Contains(a, StringComparison.OrdinalIgnoreCase));
        }

        public TimeSpan GetAge()
        {
            return DateTime.Now - Timestamp;
        }

        public bool IsRecentActivity(int minutes = 30)
        {
            return GetAge().TotalMinutes <= minutes;
        }

        private static bool DetermineRequiresReview(string action, RiskLevel riskLevel, AuditSeverity severity)
        {
            // High risk actions always require review
            if (riskLevel >= RiskLevel.High) return true;

            // Error and critical severity require review
            if (severity >= AuditSeverity.Error) return true;

            // Administrative actions require review
            var adminActions = new[] { "Delete", "Approve", "Reject", "AdminAccess", "RoleChange", "PermissionChange" };
            if (adminActions.Any(a => action.Contains(a, StringComparison.OrdinalIgnoreCase))) return true;

            // Multiple failed login attempts require review
            if (action.Contains("Login", StringComparison.OrdinalIgnoreCase) && !action.Contains("Success", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        // Override ToString for logging
        public override string ToString()
        {
            return $"AuditLog[{Id}]: {Action} on {EntityName}({EntityId}) by {UserName} at {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }

    // Enums for audit logging
    public enum AuditSeverity
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public enum RiskLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    // Audit action constants
    public static class AuditActions
    {
        // CRUD Operations
        public const string Create = "CREATE";
        public const string Read = "READ";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";

        // Authentication
        public const string Login = "LOGIN";
        public const string Logout = "LOGOUT";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string Register = "REGISTER";
        public const string PasswordChange = "PASSWORD_CHANGE";
        public const string PasswordReset = "PASSWORD_RESET";

        // Permissions & Security
        public const string RoleChange = "ROLE_CHANGE";
        public const string PermissionChange = "PERMISSION_CHANGE";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string AccountUnlocked = "ACCOUNT_UNLOCKED";
        public const string EmailVerified = "EMAIL_VERIFIED";

        // Business Operations
        public const string Approve = "APPROVE";
        public const string Reject = "REJECT";
        public const string Submit = "SUBMIT";
        public const string Download = "DOWNLOAD";
        public const string View = "VIEW";
        public const string Export = "EXPORT";
        public const string Import = "IMPORT";

        // System Operations
        public const string SystemStart = "SYSTEM_START";
        public const string SystemShutdown = "SYSTEM_SHUTDOWN";
        public const string ConfigChange = "CONFIG_CHANGE";
        public const string DatabaseMigration = "DATABASE_MIGRATION";

        // Administrative
        public const string AdminAccess = "ADMIN_ACCESS";
        public const string DataPurge = "DATA_PURGE";
        public const string Backup = "BACKUP";
        public const string Restore = "RESTORE";

        // Error Conditions
        public const string Exception = "EXCEPTION";
        public const string SecurityViolation = "SECURITY_VIOLATION";
        public const string UnauthorizedAccess = "UNAUTHORIZED_ACCESS";
        public const string SuspiciousActivity = "SUSPICIOUS_ACTIVITY";
    }
}