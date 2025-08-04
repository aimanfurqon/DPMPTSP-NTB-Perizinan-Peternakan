

// Services/LocationService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace PerizinanPeternakan.Services
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LocationService> _logger;

        // Static province mapping for quick lookups
        private static readonly Dictionary<string, string> ProvinceMapping = new()
        {
            { "Nusa Tenggara Barat", "52" },
            { "NTB", "52" },
            { "Nusa Tenggara Timur", "53" },
            { "NTT", "53" },
            { "Bali", "51" },
            { "Jawa Timur", "35" },
            { "Jatim", "35" },
            { "Jawa Tengah", "33" },
            { "Jateng", "33" },
            { "Jawa Barat", "32" },
            { "Jabar", "32" },
            { "DKI Jakarta", "31" },
            { "Jakarta", "31" },
            { "Sulawesi Selatan", "73" },
            { "Sulsel", "73" },
            { "Kalimantan Timur", "64" },
            { "Kaltim", "64" },
            { "Kalimantan Selatan", "63" },
            { "Kalsel", "63" },
            { "Sumatera Utara", "12" },
            { "Sumut", "12" },
            { "Lampung", "18" }
        };

        // Common regencies for NTB (can be expanded)
        private static readonly Dictionary<string, string> NTBRegencies = new()
        {
            { "Kota Mataram", "5271" },
            { "Kabupaten Lombok Barat", "5203" },
            { "Kabupaten Lombok Tengah", "5204" },
            { "Kabupaten Lombok Timur", "5205" },
            { "Kabupaten Lombok Utara", "5208" },
            { "Kabupaten Sumbawa", "5206" },
            { "Kabupaten Sumbawa Barat", "5209" },
            { "Kabupaten Dompu", "5207" },
            { "Kabupaten Bima", "5202" },
            { "Kota Bima", "5272" }
        };

        public LocationService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<LocationService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<SelectListItem>> GetProvincesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = "https://wilayah.id/api/provinces.json";

                _logger.LogInformation("Fetching provinces from external API: {Url}", url);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiProvinceResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (apiResponse?.Data != null)
                    {
                        var provinces = apiResponse.Data.Select(p => new SelectListItem
                        {
                            Value = $"{p.Code}|{p.Name}",
                            Text = p.Name
                        }).OrderBy(p => p.Text).ToList();

                        // Add empty option
                        provinces.Insert(0, new SelectListItem
                        {
                            Value = "",
                            Text = "-- Pilih Provinsi --"
                        });

                        _logger.LogInformation("Successfully fetched {Count} provinces from API", provinces.Count - 1);
                        return provinces;
                    }
                }

                _logger.LogWarning("API call failed or returned empty data, using fallback provinces");
                return GetFallbackProvinces();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provinces from API, using fallback data");
                return GetFallbackProvinces();
            }
        }

        public async Task<List<SelectListItem>> GetRegenciesByProvinceAsync(string provinceId)
        {
            try
            {
                if (string.IsNullOrEmpty(provinceId))
                {
                    return new List<SelectListItem>
                    {
                        new SelectListItem { Value = "", Text = "-- Pilih Provinsi Dahulu --" }
                    };
                }

                // Extract province code from "code|name" format
                var provinceCode = provinceId.Split('|')[0];

                var client = _httpClientFactory.CreateClient();
                var url = $"https://wilayah.id/api/regencies/{provinceCode}.json";

                _logger.LogInformation("Fetching regencies from external API: {Url}", url);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiRegencyResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (apiResponse?.Data != null)
                    {
                        var regencies = apiResponse.Data.Select(r => new SelectListItem
                        {
                            Value = $"{r.Code}|{r.Name}",
                            Text = r.Name
                        }).OrderBy(r => r.Text).ToList();

                        // Add empty option
                        regencies.Insert(0, new SelectListItem
                        {
                            Value = "",
                            Text = "-- Pilih Kabupaten/Kota --"
                        });

                        _logger.LogInformation("Successfully fetched {Count} regencies for province {ProvinceCode}", regencies.Count - 1, provinceCode);
                        return regencies;
                    }
                }

                _logger.LogWarning("API call failed for regencies, using fallback data for province {ProvinceCode}", provinceCode);

                // Return fallback for NTB
                if (provinceCode == "52")
                {
                    return GetFallbackNTBRegencies();
                }

                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Data tidak tersedia --" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting regencies for province {ProvinceId}", provinceId);

                // Return fallback for NTB
                var provinceCode = provinceId.Split('|')[0];
                if (provinceCode == "52")
                {
                    return GetFallbackNTBRegencies();
                }

                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Error loading data --" }
                };
            }
        }

        public async Task<List<SelectListItem>> GetPortsByProvinceAsync(string provinceCode, string? portType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(provinceCode))
                {
                    return new List<SelectListItem>
                    {
                        new SelectListItem { Value = "", Text = "-- Pilih Lokasi Dahulu --" }
                    };
                }

                var query = _context.Ports
                    .Where(p => p.ProvinceCode == provinceCode && p.IsActive);

                if (!string.IsNullOrEmpty(portType))
                {
                    query = query.Where(p => p.Type == portType);
                }

                var ports = await query
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Name,
                        Text = $"{p.Name} ({p.City})"
                    })
                    .ToListAsync();

                // Add empty option
                ports.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = "-- Pilih Pelabuhan --"
                });

                return ports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ports for province {ProvinceCode}", provinceCode);
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Error loading ports --" }
                };
            }
        }

        public async Task<List<LocationSuggestion>> GetLocationSuggestionsAsync(string query, string type)
        {
            try
            {
                if (string.IsNullOrEmpty(query) || query.Length < 2)
                {
                    return new List<LocationSuggestion>();
                }

                var suggestions = new List<LocationSuggestion>();

                if (type.ToLower() == "province")
                {
                    try
                    {
                        // Try API first
                        var provinces = await GetProvincesAsync();
                        suggestions.AddRange(provinces
                            .Where(p => !string.IsNullOrEmpty(p.Value) && p.Text.ToLower().Contains(query.ToLower()))
                            .Select(p => new LocationSuggestion
                            {
                                Code = p.Value.Split('|')[0],
                                Name = p.Text,
                                Type = "Province"
                            }));
                    }
                    catch
                    {
                        // Fallback to static data
                        suggestions.AddRange(ProvinceMapping
                            .Where(p => p.Key.ToLower().Contains(query.ToLower()))
                            .Select(p => new LocationSuggestion
                            {
                                Code = p.Value,
                                Name = p.Key,
                                Type = "Province"
                            }));
                    }
                }
                else if (type.ToLower() == "regency")
                {
                    // For regency suggestions, we need a province context
                    // Return fallback NTB regencies for now
                    suggestions.AddRange(NTBRegencies
                        .Where(r => r.Key.ToLower().Contains(query.ToLower()))
                        .Select(r => new LocationSuggestion
                        {
                            Code = r.Value,
                            Name = r.Key,
                            Type = "Regency",
                            ParentCode = "52" // NTB
                        }));
                }

                return suggestions.Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location suggestions for query: {Query}, type: {Type}", query, type);
                return new List<LocationSuggestion>();
            }
        }

        public async Task<LocationValidationResult> ValidateLocationCombinationAsync(string provinceId, string regencyId)
        {
            try
            {
                if (string.IsNullOrEmpty(provinceId) || string.IsNullOrEmpty(regencyId))
                {
                    return new LocationValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Province dan regency harus diisi"
                    };
                }

                // Extract codes from "code|name" format
                var provinceCode = provinceId.Split('|')[0];
                var regencyCode = regencyId.Split('|')[0];

                // Validate using API call
                try
                {
                    var regencies = await GetRegenciesByProvinceAsync(provinceId);
                    var isValidCombination = regencies.Any(r => r.Value.StartsWith(regencyCode + "|"));

                    if (isValidCombination)
                    {
                        return new LocationValidationResult
                        {
                            IsValid = true,
                            Message = "Valid location combination"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "API validation failed, using fallback validation");
                }

                // Fallback validation for NTB
                if (provinceCode == "52" && NTBRegencies.ContainsValue(regencyCode))
                {
                    return new LocationValidationResult
                    {
                        IsValid = true,
                        Message = "Valid location combination (fallback data)"
                    };
                }

                return new LocationValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Kombinasi provinsi dan kabupaten tidak valid"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating location combination: {ProvinceId}, {RegencyId}", provinceId, regencyId);
                return new LocationValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Terjadi kesalahan saat validasi lokasi"
                };
            }
        }

        public async Task<PortValidationResult> ValidatePortSelectionAsync(string portName, string expectedProvinceCode)
        {
            try
            {
                if (string.IsNullOrEmpty(portName))
                {
                    return new PortValidationResult
                    {
                        IsValid = true,
                        Message = "Port selection is optional"
                    };
                }

                var port = await _context.Ports
                    .FirstOrDefaultAsync(p => p.Name == portName && p.IsActive);

                if (port == null)
                {
                    return new PortValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Pelabuhan '{portName}' tidak ditemukan atau tidak aktif"
                    };
                }

                if (!string.IsNullOrEmpty(expectedProvinceCode) && port.ProvinceCode != expectedProvinceCode)
                {
                    return new PortValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Pelabuhan '{portName}' tidak sesuai dengan provinsi yang dipilih"
                    };
                }

                return new PortValidationResult
                {
                    IsValid = true,
                    Message = $"Pelabuhan '{portName}' valid",
                    ValidatedPort = port
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating port selection: {PortName}", portName);
                return new PortValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Terjadi kesalahan saat validasi pelabuhan"
                };
            }
        }

        public string ExtractProvinceCodeFromLocation(string locationString)
        {
            if (string.IsNullOrEmpty(locationString)) return "";

            try
            {
                var parts = locationString.Split(',');
                if (parts.Length >= 2)
                {
                    var province = parts.Last().Trim();

                    // Try exact match first
                    if (ProvinceMapping.ContainsKey(province))
                    {
                        return ProvinceMapping[province];
                    }

                    // Try partial match
                    foreach (var mapping in ProvinceMapping)
                    {
                        if (province.Contains(mapping.Key) || mapping.Key.Contains(province))
                        {
                            return mapping.Value;
                        }
                    }
                }

                return "52"; // Default to NTB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting province code from location: {LocationString}", locationString);
                return "52";
            }
        }

        public string ExtractRegencyCodeFromLocation(string locationString)
        {
            if (string.IsNullOrEmpty(locationString)) return "";

            try
            {
                var parts = locationString.Split(',');
                if (parts.Length >= 1)
                {
                    var regency = parts[0].Trim();

                    // Check NTB regencies
                    if (NTBRegencies.ContainsKey(regency))
                    {
                        return NTBRegencies[regency];
                    }

                    // Try partial match
                    foreach (var mapping in NTBRegencies)
                    {
                        if (regency.Contains(mapping.Key) || mapping.Key.Contains(regency))
                        {
                            return mapping.Value;
                        }
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting regency code from location: {LocationString}", locationString);
                return "";
            }
        }

        public string BuildLocationString(string regencyName, string provinceName)
        {
            if (string.IsNullOrEmpty(regencyName) || string.IsNullOrEmpty(provinceName))
            {
                return "";
            }

            return $"{regencyName.Trim()}, {provinceName.Trim()}";
        }

        public async Task<ShippingRouteValidationResult> ValidateShippingRouteAsync(
            string originProvinceCode,
            string destinationProvinceCode,
            string departurePort,
            string arrivalPort)
        {
            try
            {
                var result = new ShippingRouteValidationResult();
                var errors = new List<string>();

                // Validate departure port
                if (!string.IsNullOrEmpty(departurePort))
                {
                    var departureValidation = await ValidatePortSelectionAsync(departurePort, originProvinceCode);
                    if (!departureValidation.IsValid)
                    {
                        errors.Add($"Pelabuhan keberangkatan: {departureValidation.ErrorMessage}");
                    }
                    else
                    {
                        result.ValidatedDeparturePort = departureValidation.ValidatedPort;
                    }
                }

                // Validate arrival port
                if (!string.IsNullOrEmpty(arrivalPort))
                {
                    var arrivalValidation = await ValidatePortSelectionAsync(arrivalPort, destinationProvinceCode);
                    if (!arrivalValidation.IsValid)
                    {
                        errors.Add($"Pelabuhan tujuan: {arrivalValidation.ErrorMessage}");
                    }
                    else
                    {
                        result.ValidatedArrivalPort = arrivalValidation.ValidatedPort;
                    }
                }

                // Business rule: Check if route is logical
                if (originProvinceCode == destinationProvinceCode)
                {
                    result.Warnings.Add("Asal dan tujuan berada di provinsi yang sama. Pastikan pelabuhan yang dipilih sesuai dengan rute pengiriman.");
                }

                result.IsValid = !errors.Any();
                result.ErrorMessages = errors;

                if (result.IsValid)
                {
                    result.Message = "Rute pengiriman valid";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating shipping route");
                return new ShippingRouteValidationResult
                {
                    IsValid = false,
                    ErrorMessages = new List<string> { "Terjadi kesalahan saat validasi rute pengiriman" }
                };
            }
        }

        public async Task<Province?> GetProvinceByCodeAsync(string provinceCode)
        {
            try
            {
                // Since we're using external API, we'll create a Province object on-the-fly
                var provinces = await GetProvincesAsync();
                var province = provinces.FirstOrDefault(p => p.Value.StartsWith(provinceCode + "|"));

                if (province != null)
                {
                    var parts = province.Value.Split('|');
                    return new Province
                    {
                        Code = parts[0],
                        Name = parts[1],
                        IsActive = true
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting province by code: {ProvinceCode}", provinceCode);
                return null;
            }
        }

        public async Task<Regency?> GetRegencyByCodeAsync(string regencyCode)
        {
            try
            {
                // This is more complex with external API since we need province context
                // For now, return null or implement if needed
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting regency by code: {RegencyCode}", regencyCode);
                return null;
            }
        }

        public async Task<Port?> GetPortByNameAsync(string portName)
        {
            try
            {
                return await _context.Ports
                    .FirstOrDefaultAsync(p => p.Name == portName && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting port by name: {PortName}", portName);
                return null;
            }
        }

        public async Task<LocationParseResult> ParseAndValidateLocationDataAsync(LocationFormData locationData)
        {
            try
            {
                var result = new LocationParseResult();
                var errors = new List<string>();

                // Parse and validate origin location
                if (!string.IsNullOrEmpty(locationData.OriginProvinceId) && !string.IsNullOrEmpty(locationData.OriginRegencyId))
                {
                    var originValidation = await ValidateLocationCombinationAsync(locationData.OriginProvinceId, locationData.OriginRegencyId);
                    if (originValidation.IsValid)
                    {
                        result.ParsedOriginLocation = BuildLocationString(
                            locationData.OriginRegencyName ?? "",
                            locationData.OriginProvinceName ?? ""
                        );
                        result.OriginProvinceCode = locationData.OriginProvinceId.Split('|')[0];
                        result.OriginRegencyCode = locationData.OriginRegencyId.Split('|')[0];
                    }
                    else
                    {
                        errors.Add($"Lokasi asal tidak valid: {originValidation.ErrorMessage}");
                    }
                }

                // Parse and validate destination location
                if (!string.IsNullOrEmpty(locationData.DestinationProvinceId) && !string.IsNullOrEmpty(locationData.DestinationRegencyId))
                {
                    var destinationValidation = await ValidateLocationCombinationAsync(locationData.DestinationProvinceId, locationData.DestinationRegencyId);
                    if (destinationValidation.IsValid)
                    {
                        result.ParsedDestinationLocation = BuildLocationString(
                            locationData.DestinationRegencyName ?? "",
                            locationData.DestinationProvinceName ?? ""
                        );
                        result.DestinationProvinceCode = locationData.DestinationProvinceId.Split('|')[0];
                        result.DestinationRegencyCode = locationData.DestinationRegencyId.Split('|')[0];
                    }
                    else
                    {
                        errors.Add($"Lokasi tujuan tidak valid: {destinationValidation.ErrorMessage}");
                    }
                }

                // Validate shipping route if ports are provided
                if (!string.IsNullOrEmpty(locationData.DeparturePort) || !string.IsNullOrEmpty(locationData.ArrivalPort))
                {
                    var routeValidation = await ValidateShippingRouteAsync(
                        result.OriginProvinceCode ?? "",
                        result.DestinationProvinceCode ?? "",
                        locationData.DeparturePort ?? "",
                        locationData.ArrivalPort ?? ""
                    );

                    if (!routeValidation.IsValid)
                    {
                        errors.AddRange(routeValidation.ErrorMessages);
                    }
                    else
                    {
                        result.ValidatedDeparturePort = routeValidation.ValidatedDeparturePort;
                        result.ValidatedArrivalPort = routeValidation.ValidatedArrivalPort;
                        result.RouteWarnings = routeValidation.Warnings;
                    }
                }

                result.IsValid = !errors.Any();
                result.ValidationErrors = errors;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing and validating location data");
                return new LocationParseResult
                {
                    IsValid = false,
                    ValidationErrors = new List<string> { "Terjadi kesalahan saat memproses data lokasi" }
                };
            }
        }

        #region Private Helper Methods

        private List<SelectListItem> GetFallbackProvinces()
        {
            var provinces = ProvinceMapping.Select(p => new SelectListItem
            {
                Value = $"{p.Value}|{p.Key}",
                Text = p.Key
            }).OrderBy(p => p.Text).ToList();

            provinces.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Pilih Provinsi --"
            });

            return provinces;
        }

        private List<SelectListItem> GetFallbackNTBRegencies()
        {
            var regencies = NTBRegencies.Select(r => new SelectListItem
            {
                Value = $"{r.Value}|{r.Key}",
                Text = r.Key
            }).OrderBy(r => r.Text).ToList();

            regencies.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Pilih Kabupaten/Kota --"
            });

            return regencies;
        }

        #endregion
    }

    // API Response Models untuk wilayah.id
    public class ApiProvinceResponse
    {
        public List<ApiProvince> Data { get; set; } = new();
    }

    public class ApiProvince
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ApiRegencyResponse
    {
        public List<ApiRegency> Data { get; set; } = new();
    }

    public class ApiRegency
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProvinceCode { get; set; } = string.Empty;
    }
}

