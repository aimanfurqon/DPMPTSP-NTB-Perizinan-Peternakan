namespace PerizinanPeternakan.Models
{
    public class DocumentUploadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int UploadedCount { get; set; }
        public List<PermitDocument> UploadedDocuments { get; set; } = new();
    }

    public class DocumentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class DocumentUpdateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public PermitDocument? UpdatedDocument { get; set; }
    }
}