using PerizinanPeternakan.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PerizinanPeternakan.Services
{
    public interface ILocationService
    {
        /// <summary>
        /// Get all active provinces
        /// </summary>
        /// <returns>List of provinces for dropdown</returns>
        Task<List<SelectListItem>> GetProvincesAsync();

        /// <summary>
        /// Get regencies by province ID
        /// </summary>
        /// <param name="provinceId">Province ID</param>
        /// <returns>List of regencies for dropdown</returns>
        Task<List<SelectListItem>> GetRegenciesByProvinceAsync(string provinceId);

        /// <summary>
        /// Get ports by province code
        /// </summary>
        /// <param name="provinceCode">Province code</param>
        /// <param name="portType">Optional port type filter</param>
        /// <returns>List of ports for dropdown</returns>
        Task<List<SelectListItem>> GetPortsByProvinceAsync(string provinceCode, string? portType = null);

        /// <summary>
        /// Get location suggestions for autocomplete
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="type">Location type (province/regency)</param>
        /// <returns>Location suggestions</returns>
        Task<List<LocationSuggestion>> GetLocationSuggestionsAsync(string query, string type);

        /// <summary>
        /// Validate location combination (province + regency)
        /// </summary>
        /// <param name="provinceId">Province ID</param>
        /// <param name="regencyId">Regency ID</param>
        /// <returns>Validation result</returns>
        Task<LocationValidationResult> ValidateLocationCombinationAsync(string provinceId, string regencyId);

        /// <summary>
        /// Validate port selection based on province
        /// </summary>
        /// <param name="portName">Port name</param>
        /// <param name="expectedProvinceCode">Expected province code</param>
        /// <returns>Validation result</returns>
        Task<PortValidationResult> ValidatePortSelectionAsync(string portName, string expectedProvinceCode);

        /// <summary>
        /// Extract province code from location string
        /// </summary>
        /// <param name="locationString">Location string (e.g., "Mataram, Nusa Tenggara Barat")</param>
        /// <returns>Province code</returns>
        string ExtractProvinceCodeFromLocation(string locationString);

        /// <summary>
        /// Extract regency code from location string
        /// </summary>
        /// <param name="locationString">Location string</param>
        /// <returns>Regency code</returns>
        string ExtractRegencyCodeFromLocation(string locationString);

        /// <summary>
        /// Build full location string from components
        /// </summary>
        /// <param name="regencyName">Regency name</param>
        /// <param name="provinceName">Province name</param>
        /// <returns>Formatted location string</returns>
        string BuildLocationString(string regencyName, string provinceName);

        /// <summary>
        /// Validate shipping route (origin to destination with ports)
        /// </summary>
        /// <param name="originProvinceCode">Origin province code</param>
        /// <param name="destinationProvinceCode">Destination province code</param>
        /// <param name="departurePort">Departure port name</param>
        /// <param name="arrivalPort">Arrival port name</param>
        /// <returns>Route validation result</returns>
        Task<ShippingRouteValidationResult> ValidateShippingRouteAsync(
            string originProvinceCode,
            string destinationProvinceCode,
            string departurePort,
            string arrivalPort);

        /// <summary>
        /// Get province by code
        /// </summary>
        /// <param name="provinceCode">Province code</param>
        /// <returns>Province information</returns>
        Task<Province?> GetProvinceByCodeAsync(string provinceCode);

        /// <summary>
        /// Get regency by code
        /// </summary>
        /// <param name="regencyCode">Regency code</param>
        /// <returns>Regency information</returns>
        Task<Regency?> GetRegencyByCodeAsync(string regencyCode);

        /// <summary>
        /// Get port by name
        /// </summary>
        /// <param name="portName">Port name</param>
        /// <returns>Port information</returns>
        Task<Port?> GetPortByNameAsync(string portName);

        /// <summary>
        /// Parse and validate location data from form input
        /// </summary>
        /// <param name="locationData">Location data from form</param>
        /// <returns>Parsed and validated location result</returns>
        Task<LocationParseResult> ParseAndValidateLocationDataAsync(LocationFormData locationData);
    }
}