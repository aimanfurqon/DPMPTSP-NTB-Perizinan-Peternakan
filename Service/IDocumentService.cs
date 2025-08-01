using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Service
{
    public interface IDocumentService
    {
        Task<DocumentUploadResult> UploadSupportingDocumentsAsync(int permitId, PermitApplicationViewModel model, int userId);

        DocumentValidationResult ValidateUploadedFile(IFormFile file);

        DocumentValidationResult ValidateAllRequiredDocuments(PermitApplicationViewModel model);

        DocumentValidationResult ValidateDocumentDetails(PermitApplicationViewModel model);

        Task<PermitDocument?> GetDocumentWithAuthorizationAsync(int documentId, int userId, string userRole);

        Task<int> CleanupOrphanedFilesAsync();

        string GetContentType(string fileExtension);

        Task<DocumentUpdateResult> UpdateDocumentDetailsAsync(int documentId, DateTime? documentDate, string? documentNumber, string? documentDescription);
    }
}
