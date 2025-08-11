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
    public class ApplicationNumberService : IApplicationNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationNumberService> _logger;

        // Constants for application number format
        private const string APPLICATION_NUMBER_PATTERN = @"^(\d{3})/03-260/DPM&PTSP/(\d{4})$";
        private const string APPLICATION_NUMBER_SUFFIX = "/03-260/DPM&PTSP";
        private const int MAX_RETRY_ATTEMPTS = 20; // Increased from 10 to 20

        public ApplicationNumberService(ApplicationDbContext context, ILogger<ApplicationNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateApplicationNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            _logger.LogInformation("Starting application number generation for {Year}-{Month}", year, month);

            // First, try without transaction to check if there are any existing applications
            try
            {
                var existingCount = await _context.PermitApplications
                    .Where(p => p.SubmissionDate.Year == year && p.SubmissionDate.Month == month)
                    .CountAsync();

                _logger.LogInformation("Found {Count} existing applications for {Year}-{Month}", existingCount, year, month);

                // If no existing applications, start with 1
                if (existingCount == 0)
                {
                    var applicationNumber = FormatApplicationNumber(1, year);
                    _logger.LogInformation("No existing applications, using first number: {ApplicationNumber}", applicationNumber);
                    return applicationNumber;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking existing applications, proceeding with normal flow");
            }

            // Use transaction for generating new numbers
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Try to get a lock on the table
                await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PermitApplications WITH (TABLOCKX)");

                var nextNumber = await GetNextSequenceNumberForPeriodAsync(year, month);
                _logger.LogInformation("Starting with sequence number: {NextNumber} for {Year}-{Month}", nextNumber, year, month);

                string applicationNumber;
                int retryCount = 0;

                do
                {
                    applicationNumber = FormatApplicationNumber(nextNumber, year);
                    _logger.LogDebug("Trying application number: {ApplicationNumber}", applicationNumber);

                    var exists = await ApplicationNumberExistsAsync(applicationNumber);

                    if (!exists)
                    {
                        _logger.LogInformation("Generated unique application number: {ApplicationNumber}", applicationNumber);
                        break; 
                    }

                    nextNumber++;
                    retryCount++;

                    _logger.LogWarning("Application number {ApplicationNumber} already exists, retrying with {NextNumber} (attempt {RetryCount})",
                        applicationNumber, nextNumber, retryCount);

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        var errorMessage = $"Failed to generate unique application number after {MAX_RETRY_ATTEMPTS} attempts. Last tried: {applicationNumber}";
                        _logger.LogError(errorMessage);
                        
                        // Try to get a completely new number by checking the database again
                        var maxNumber = await GetMaxSequenceNumberForPeriodAsync(year, month);
                        var newNumber = maxNumber + 1;
                        applicationNumber = FormatApplicationNumber(newNumber, year);
                        
                        _logger.LogInformation("Trying alternative approach with number: {ApplicationNumber}", applicationNumber);
                        
                        if (!await ApplicationNumberExistsAsync(applicationNumber))
                        {
                            _logger.LogInformation("Successfully generated alternative application number: {ApplicationNumber}", applicationNumber);
                            break;
                        }
                        
                        throw new InvalidOperationException(errorMessage);
                    }

                } while (true);

                await transaction.CommitAsync();

                _logger.LogInformation("Successfully generated application number: {ApplicationNumber}", applicationNumber);
                return applicationNumber;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error generating application number for {Year}-{Month}", year, month);
                throw;
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

                int maxNumber = 0;
                foreach (var appNumber in applications)
                {
                    var parts = appNumber.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int number))
                    {
                        maxNumber = Math.Max(maxNumber, number);
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
