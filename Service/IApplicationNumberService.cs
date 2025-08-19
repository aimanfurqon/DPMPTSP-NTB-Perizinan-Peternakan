namespace PerizinanPeternakan.Service
{
    /// <summary>
    /// Interface for generating and managing application numbers for livestock permit applications.
    /// </summary>
    public interface IApplicationNumberService
    {
        /// <summary>
        /// Generates a unique application number for the current month and year.
        /// </summary>
        /// <returns>A unique application number string.</returns>
        Task<string> GenerateApplicationNumberAsync();

        /// <summary>
        /// Validates if the provided application number format is correct.
        /// </summary>
        /// <param name="applicationNumber">The application number to validate.</param>
        /// <returns>True if the format is valid, false otherwise.</returns>
        bool IsValidApplicationNumberFormat(string applicationNumber);

        /// <summary>
        /// Gets the next sequence number for application number generation.
        /// </summary>
        /// <returns>The next sequence number.</returns>
        Task<int> GetNextSequenceNumberAsync();

        /// <summary>
        /// Checks if an application number already exists in the database.
        /// </summary>
        /// <param name="applicationNumber">The application number to check.</param>
        /// <returns>True if the application number exists, false otherwise.</returns>
        Task<bool> ApplicationNumberExistsAsync(string applicationNumber);
    }
}
