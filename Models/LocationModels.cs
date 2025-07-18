using System.ComponentModel.DataAnnotations;

namespace PerizinanPeternakan.Models
{
    public class Province
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Regency> Regencies { get; set; } = new List<Regency>();
    }

    public class Regency
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
        public virtual Province Province { get; set; }
    }
}