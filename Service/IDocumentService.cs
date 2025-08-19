using PerizinanPeternakan.Models;
using PerizinanPeternakan.ViewModels;

namespace PerizinanPeternakan.Service
{
    /// <summary>
    /// Interface for managing document uploads, validation, and operations for permit applications.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Uploads supporting documents for a permit application.
        /// </summary>
        /// <param name="permitId">The ID of the permit application.</param>
        /// <param name="model">The permit application view model containing document information.</param>
        /// <param name="userId">The ID of the user uploading the documents.</param>
        /// <returns>The result of the document upload operation.</returns>
        Task<DocumentUploadResult> UploadSupportingDocumentsAsync(int permitId, PermitApplicationViewModel model, int userId);

        /// <summary>
        /// Validates an uploaded file for format, size, and security.
        /// </summary>
        /// <param name="file">The file to validate.</param>
        /// <returns>The validation result.</returns>
        DocumentValidationResult ValidateUploadedFile(IFormFile file);

        /// <summary>
        /// Validates that all required documents are present in the application.
        /// </summary>
        /// <param name="model">The permit application view model to validate.</param>
        /// <returns>The validation result.</returns>
        DocumentValidationResult ValidateAllRequiredDocuments(PermitApplicationViewModel model);

        /// <summary>
        /// Validates the details of documents in the application.
        /// </summary>
        /// <param name="model">The permit application view model to validate.</param>
        /// <returns>The validation result.</returns>
        DocumentValidationResult ValidateDocumentDetails(PermitApplicationViewModel model);

        /// <summary>
        /// Gets a document with authorization check for the specified user.
        /// </summary>
        /// <param name="documentId">The ID of the document to retrieve.</param>
        /// <param name="userId">The ID of the user requesting the document.</param>
        /// <param name="userRole">The role of the user requesting the document.</param>
        /// <returns>The document if authorized, null otherwise.</returns>
        Task<PermitDocument?> GetDocumentWithAuthorizationAsync(int documentId, int userId, string userRole);

        /// <summary>
        /// Cleans up orphaned files that are no longer referenced in the database.
        /// </summary>
        /// <returns>The number of files cleaned up.</returns>
        Task<int> CleanupOrphanedFilesAsync();

        /// <summary>
        /// Gets the content type for a given file extension.
        /// </summary>
        /// <param name="fileExtension">The file extension to get content type for.</param>
        /// <returns>The content type string.</returns>
        string GetContentType(string fileExtension);

        /// <summary>
        /// Updates the details of a document.
        /// </summary>
        /// <param name="documentId">The ID of the document to update.</param>
        /// <param name="documentDate">The document date.</param>
        /// <param name="documentNumber">The document number.</param>
        /// <param name="documentDescription">The document description.</param>
        /// <returns>The result of the update operation.</returns>
        Task<DocumentUpdateResult> UpdateDocumentDetailsAsync(int documentId, DateTime? documentDate, string? documentNumber, string? documentDescription);
    }
}
