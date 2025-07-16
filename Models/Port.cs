// Tambahkan model ini ke folder Models/Port.cs
using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.Models
{
    public class Port
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Province { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string ProvinceCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string Type { get; set; } = "Ferry";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}