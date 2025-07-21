using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Services
{
    public interface IDocumentService
    {
        // Document Upload Operations
        Task<(bool Success, string ErrorMessage, int UploadedCount)> UploadSupportingDocumentsAsync(
            int permitId, PermitApplicationViewModel model, int userId);

        // Document Validation
        (bool IsValid, List<string> Errors) ValidateUploadedFile(IFormFile file);
        bool IsValidFile(IFormFile file);
        bool ValidateAllDocumentsUploaded(PermitApplicationViewModel model);
        bool ValidateDocumentDetails(PermitApplicationViewModel model);

        // Document Management
        Task<DocumentViewModel?> GetDocumentAsync(int documentId, string userRole, int userId);
        Task<(bool Success, string ErrorMessage)> UpdateDocumentDetailsAsync(
            int documentId, DocumentDetailsViewModel model, int userId);
        Task<(bool Success, string Message, int UpdatedCount)> BulkUpdateDocumentDetailsAsync(
            List<DocumentDetailsViewModel> documentDetails, int userId);

        // Document Access and Download
        Task<(bool CanAccess, byte[]? FileBytes, string? ContentType, string? FileName)>
            GetDocumentForDownloadAsync(int documentId, string userRole, int userId);
        string GetContentType(string fileExtension);

        // Document Content Generation
        Task<string> GetDocumentContentAsync(LivestockPermitApplication permit);
        Task<byte[]> GetDocumentFileAsync(int permitId, string userRole, int userId);

        // Helper Methods
        string GenerateDocumentDescription(string name, string? number, DateTime? date);
        bool IsValidDocumentNumber(string documentNumber);
        string FormatFileSize(long bytes);
        void CleanupUploadedFiles(List<string> filePaths);

        // Document Details Validation API
        Task<List<object>> ValidateDocumentDetailsAPIAsync(List<DocumentDetailsViewModel> documentDetails);
    }
}