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
        private const int MAX_RETRY_ATTEMPTS = 10;

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

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM PermitApplications WITH (TABLOCKX)");

                var nextNumber = await GetNextSequenceNumberForPeriodAsync(year, month);

                string applicationNumber;
                int retryCount = 0;

                do
                {
                    applicationNumber = FormatApplicationNumber(nextNumber, year);

                    var exists = await ApplicationNumberExistsAsync(applicationNumber);

                    if (!exists)
                    {
                        _logger.LogInformation("Generated unique application number: {ApplicationNumber}", applicationNumber);
                        break; 
                    }

                    nextNumber++;
                    retryCount++;

                    _logger.LogWarning("Application number {ApplicationNumber} already exists, retrying with {NextNumber}",
                        applicationNumber, nextNumber);

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        var errorMessage = $"Failed to generate unique application number after {MAX_RETRY_ATTEMPTS} attempts";
                        _logger.LogError(errorMessage);
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
                var lastApplication = await _context.PermitApplications
                    .Where(p => p.ApplicationNumber.EndsWith($"{APPLICATION_NUMBER_SUFFIX}/{year}") &&
                               p.SubmissionDate.Year == year &&
                               p.SubmissionDate.Month == month)
                    .OrderByDescending(p => p.ApplicationNumber)
                    .FirstOrDefaultAsync();

                if (lastApplication == null)
                {
                    _logger.LogInformation("No existing applications found for {Year}-{Month}, starting with 1", year, month);
                    return 1;
                }

                var numberPart = lastApplication.ApplicationNumber.Split('/')[0];
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    var nextNumber = currentNumber + 1;
                    _logger.LogInformation("Last application number for {Year}-{Month}: {LastNumber}, next: {NextNumber}",
                        year, month, currentNumber, nextNumber);
                    return nextNumber;
                }

                _logger.LogWarning("Could not parse sequence number from {ApplicationNumber}, starting with 1",
                    lastApplication.ApplicationNumber);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next sequence number for {Year}-{Month}", year, month);
                return 1; 
            }
        }

        private static string FormatApplicationNumber(int sequenceNumber, int year)
        {
            return $"{sequenceNumber.ToString().PadLeft(3, '0')}{APPLICATION_NUMBER_SUFFIX}/{year}";
        }

        #endregion
    }
}
