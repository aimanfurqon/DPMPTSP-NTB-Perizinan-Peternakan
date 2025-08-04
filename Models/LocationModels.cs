using System.ComponentModel.DataAnnotations;
    
namespace PerizinanPeternakan.Models
{
    //public class Province
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public virtual ICollection<Regency> Regencies { get; set; } = new List<Regency>();
    //}
    //public class Regency
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public int ProvinceId { get; set; }
    //    public virtual Province Province { get; set; }
    //}

    public class LocationSuggestion
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
    }

    public class LocationValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Regency? ValidatedRegency { get; set; }
    }

    public class PortValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Port? ValidatedPort { get; set; }
    }

    public class ShippingRouteValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ErrorMessages { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Port? ValidatedDeparturePort { get; set; }
        public Port? ValidatedArrivalPort { get; set; }
    }

    public class LocationFormData
    {
        public string? OriginProvinceId { get; set; }
        public string? OriginProvinceName { get; set; }
        public string? OriginRegencyId { get; set; }
        public string? OriginRegencyName { get; set; }
        public string? DestinationProvinceId { get; set; }
        public string? DestinationProvinceName { get; set; }
        public string? DestinationRegencyId { get; set; }
        public string? DestinationRegencyName { get; set; }
        public string? DeparturePort { get; set; }
        public string? ArrivalPort { get; set; }
    }

    public class LocationParseResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> RouteWarnings { get; set; } = new();

        public string? ParsedOriginLocation { get; set; }
        public string? ParsedDestinationLocation { get; set; }
        public string? OriginProvinceCode { get; set; }
        public string? OriginRegencyCode { get; set; }
        public string? DestinationProvinceCode { get; set; }
        public string? DestinationRegencyCode { get; set; }

        public Port? ValidatedDeparturePort { get; set; }
        public Port? ValidatedArrivalPort { get; set; }
    }

    // Add these models if they don't exist in your project
    public class Province
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class Regency
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProvinceCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    //public class Port
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public string City { get; set; } = string.Empty;
    //    public string Province { get; set; } = string.Empty;
    //    public string ProvinceCode { get; set; } = string.Empty;
    //    public string Type { get; set; } = string.Empty;
    //    public bool IsActive { get; set; } = true;
    //}
} 