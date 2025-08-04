namespace PerizinanPeternakan.Models
{
    public class ApprovalResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public PermitStatus NewStatus { get; set; }
        public string PermitApplicationNumber { get; set; } = string.Empty;
        public TimeSpan ProcessingTime { get; set; }
        public string? GeneratedDocumentPath { get; set; }
    }

    public class BulkApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public List<string> SuccessfulPermits { get; set; } = new();
        public List<BulkFailure> FailedPermits { get; set; } = new();
    }

    public class BulkFailure
    {
        public string PermitNumber { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ApprovalWithEditResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public PermitStatus NewStatus { get; set; }
        public string PermitApplicationNumber { get; set; } = string.Empty;
        public List<string> ChangedFields { get; set; } = new();
        public Dictionary<string, string> OriginalData { get; set; } = new();
    }

    public class ApprovalStatistics
    {
        public int UserId { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public int TotalReviewed { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int ThisMonthReviewed { get; set; }
        public double AverageProcessingDays { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double ApprovalRate => TotalReviewed > 0 ? (double)TotalApproved / TotalReviewed * 100 : 0;
    }

    public class ApprovalTimelineItem
    {
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public PermitStatus FromStatus { get; set; }
        public PermitStatus ToStatus { get; set; }
        public bool IsApproval { get; set; }
        public bool IsRejection { get; set; }
        public string TimeAgo => CalculateTimeAgo(Date);

        private string CalculateTimeAgo(DateTime date)
        {
            var timespan = DateTime.Now - date;
            if (timespan.TotalDays >= 1)
                return $"{(int)timespan.TotalDays} hari yang lalu";
            if (timespan.TotalHours >= 1)
                return $"{(int)timespan.TotalHours} jam yang lalu";
            if (timespan.TotalMinutes >= 1)
                return $"{(int)timespan.TotalMinutes} menit yang lalu";
            return "Baru saja";
        }
    }

    public class ApprovalValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationDetails { get; set; } = new();
    }

    public class AutoAdvanceResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public PermitStatus? NewStatus { get; set; }
        public string? ActionTaken { get; set; }
    }
}