using PerizinanPeternakan.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient; 
using System.Linq;

namespace PerizinanPeternakan.Service
{
    /// <summary>
    /// Service for generating and managing application numbers for livestock permit applications.
    /// </summary>
    public class ApplicationNumberService : IApplicationNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationNumberService> _logger;

        // Constants for application number format
        private const string APPLICATION_NUMBER_PATTERN = @"^(\d{3})/03-260/DPM&PTSP/(\d{4})$";
        private const string APPLICATION_NUMBER_SUFFIX = "/03-260/DPM&PTSP";
        private const int MAX_RETRY_ATTEMPTS = 20; // Increased from 10 to 20

        /// <summary>
        /// Initializes a new instance of the ApplicationNumberService class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="logger">The logger instance.</param>
        public ApplicationNumberService(ApplicationDbContext context, ILogger<ApplicationNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generates a unique application number for the current month and year.
        /// </summary>
        /// <returns>A unique application number string.</returns>
        public async Task<string> GenerateApplicationNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            _logger.LogInformation("Starting application number generation for {Year}-{Month}", year, month);

            // Try multiple approaches to ensure uniqueness
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _logger.LogInformation("Attempt {Attempt} to generate application number", attempt);
                    
                    // Approach 1: Use database transaction with table lock
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Lock the table to prevent race conditions
                        await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PermitApplications WITH (TABLOCKX)");

                        // Get all existing application numbers for this period
                        var existingNumbers = await _context.PermitApplications
                            .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                            .Select(p => p.ApplicationNumber)
                            .ToListAsync();

                        _logger.LogInformation("Found {Count} existing applications for {Year}-{Month}", existingNumbers.Count, year, month);

                        // If no existing applications, start with 1
                        if (!existingNumbers.Any())
                        {
                            var applicationNumber = FormatApplicationNumber(1, year);
                            _logger.LogInformation("No existing applications, using first number: {ApplicationNumber}", applicationNumber);
                            
                            // Final validation before commit
                            var finalCheck = await _context.PermitApplications
                                .Where(p => p.ApplicationNumber == applicationNumber)
                                .AnyAsync();
                            
                            if (!finalCheck)
                            {
                                await transaction.CommitAsync();
                                return applicationNumber;
                            }
                            else
                            {
                                _logger.LogWarning("Application number {ApplicationNumber} already exists during final check", applicationNumber);
                                await transaction.RollbackAsync();
                                continue; // Try next attempt
                            }
                        }

                        // Find the maximum sequence number
                        int maxNumber = 0;
                        foreach (var appNumber in existingNumbers)
                        {
                            if (string.IsNullOrWhiteSpace(appNumber))
                                continue;

                            var parts = appNumber.Split('/');
                            if (parts.Length > 0 && int.TryParse(parts[0], out int number))
                            {
                                maxNumber = Math.Max(maxNumber, number);
                            }
                        }

                        var nextNumber = maxNumber + 1;
                        _logger.LogInformation("Max sequence number for {Year}-{Month}: {MaxNumber}, next: {NextNumber}", 
                            year, month, maxNumber, nextNumber);

                        // Try to generate a unique number with retry logic
                        string generatedNumber;
                        int retryCount = 0;
                        const int maxRetries = 50; // Reduced retry attempts per attempt

                        do
                        {
                            generatedNumber = FormatApplicationNumber(nextNumber, year);
                            _logger.LogDebug("Trying application number: {ApplicationNumber} (attempt {RetryCount})", 
                                generatedNumber, retryCount + 1);

                            // Check if this number already exists in our current list
                            if (!existingNumbers.Contains(generatedNumber))
                            {
                                // Final validation before commit
                                var finalCheck = await _context.PermitApplications
                                    .Where(p => p.ApplicationNumber == generatedNumber)
                                    .AnyAsync();
                                
                                if (!finalCheck)
                                {
                                    _logger.LogInformation("Generated unique application number: {ApplicationNumber}", generatedNumber);
                                    await transaction.CommitAsync();
                                    return generatedNumber;
                                }
                                else
                                {
                                    _logger.LogWarning("Application number {ApplicationNumber} already exists during final check", generatedNumber);
                                    existingNumbers.Add(generatedNumber); // Add to our list to avoid retrying
                                }
                            }

                            _logger.LogWarning("Application number {ApplicationNumber} already exists, trying next number", generatedNumber);
                            nextNumber++;
                            retryCount++;

                            if (retryCount >= maxRetries)
                            {
                                break; // Try next attempt
                            }

                        } while (true);

                        await transaction.RollbackAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error in attempt {Attempt} with transaction", attempt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in attempt {Attempt}", attempt);
                }
            }

            // If all attempts failed, use timestamp-based fallback
            _logger.LogWarning("All attempts failed, using timestamp-based fallback");
            try
            {
                var timestamp = DateTime.Now.ToString("HHmmss");
                var fallbackNumber = $"999{timestamp}/03-260/DPM&PTSP/{year}";
                _logger.LogWarning("Using timestamp-based fallback number: {FallbackNumber}", fallbackNumber);
                return fallbackNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Even fallback application number generation failed");
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"999{timestamp}/03-260/DPM&PTSP/{year}";
            }
        }

        public async Task<int> GetNextSequenceNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            return await GetNextSequenceNumberForPeriodAsync(year, month);
        }

        public bool IsValidApplicationNumberFormat(string applicationNumber)
        {
            if (string.IsNullOrWhiteSpace(applicationNumber))
                return false;

            return Regex.IsMatch(applicationNumber, APPLICATION_NUMBER_PATTERN);
        }

        public async Task<bool> ApplicationNumberExistsAsync(string applicationNumber)
        {
            if (string.IsNullOrWhiteSpace(applicationNumber))
                return false;

            return await _context.PermitApplications
                .AnyAsync(p => p.ApplicationNumber == applicationNumber);
        }

        #region Private Helper Methods

        private async Task<int> GetNextSequenceNumberForPeriodAsync(int year, int month)
        {
            try
            {
                // First, try to find the last application number for this period
                var lastApplication = await _context.PermitApplications
                    .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                    .OrderByDescending(p => p.ApplicationNumber)
                    .FirstOrDefaultAsync();

                if (lastApplication == null)
                {
                    _logger.LogInformation("No existing applications found for {Year}-{Month}, starting with 1", year, month);
                    return 1;
                }

                // Try to parse the sequence number from the application number
                var numberPart = lastApplication.ApplicationNumber.Split('/')[0];
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    var nextNumber = currentNumber + 1;
                    _logger.LogInformation("Last application number for {Year}-{Month}: {LastNumber}, next: {NextNumber}",
                        year, month, currentNumber, nextNumber);
                    return nextNumber;
                }

                // If parsing fails, try to get the maximum number from all applications for this period
                var maxNumber = await GetMaxSequenceNumberForPeriodAsync(year, month);
                var calculatedNextNumber = maxNumber + 1;
                
                _logger.LogWarning("Could not parse sequence number from {ApplicationNumber}, using max + 1: {NextNumber}",
                    lastApplication.ApplicationNumber, calculatedNextNumber);
                return calculatedNextNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next sequence number for {Year}-{Month}", year, month);
                return 1; 
            }
        }

        private async Task<int> GetMaxSequenceNumberForPeriodAsync(int year, int month)
        {
            try
            {
                var applications = await _context.PermitApplications
                    .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                    .Select(p => p.ApplicationNumber)
                    .ToListAsync();

                if (!applications.Any())
                {
                    _logger.LogInformation("No applications found for {Year}-{Month}, returning 0", year, month);
                    return 0;
                }

                int maxNumber = 0;
                foreach (var appNumber in applications)
                {
                    if (string.IsNullOrWhiteSpace(appNumber))
                        continue;

                    var parts = appNumber.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int number))
                    {
                        maxNumber = Math.Max(maxNumber, number);
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse sequence number from application number: {ApplicationNumber}", appNumber);
                    }
                }

                _logger.LogInformation("Max sequence number found for {Year}-{Month}: {MaxNumber}", year, month, maxNumber);
                return maxNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max sequence number for {Year}-{Month}", year, month);
                return 0;
            }
        }

        private static string FormatApplicationNumber(int sequenceNumber, int year)
        {
            return $"{sequenceNumber.ToString().PadLeft(3, '0')}{APPLICATION_NUMBER_SUFFIX}/{year}";
        }

        #endregion
    }
}
