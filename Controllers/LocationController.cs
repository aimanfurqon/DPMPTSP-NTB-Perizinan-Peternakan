using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace PerizinanPeternakan.Controllers
{
    public class LocationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LocationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> GetProvinces()
        {
            var client = _httpClientFactory.CreateClient();
            var url = "https://wilayah.id/api/provinces.json";

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();  
                    return Content(jsonString, "application/json");
                }
            }
            catch (HttpRequestException e)
            {
                return StatusCode(503, new { message = "Layanan data wilayah tidak tersedia.", error = e.Message });
            }

            return StatusCode(500, new { message = "Terjadi kesalahan saat mengambil data provinsi." });
        }

        public async Task<IActionResult> GetRegencies(string provinceId) 
        {
            if (string.IsNullOrEmpty(provinceId))
            {
                return BadRequest(new { message = "ID Provinsi diperlukan." });
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://wilayah.id/api/regencies/{provinceId}.json";

            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return Content(jsonString, "application/json");
                }
            }
            catch (HttpRequestException e)
            {
                return StatusCode(503, new { message = "Layanan data wilayah tidak tersedia.", error = e.Message });
            }


            return StatusCode(500, new { message = "Terjadi kesalahan saat mengambil data kabupaten/kota." });
        }

        public async Task<IActionResult> GetDistricts(string regencyId)
        {
            if (string.IsNullOrEmpty(regencyId))
            {
                return BadRequest(new { message = "ID Kabupaten/Kota diperlukan." });
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://wilayah.id/api/districts/{regencyId}.json";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return Content(jsonString, "application/json");
            }
            return StatusCode(500, new { message = "Gagal mengambil data kecamatan." });
        }

        // GET: /Location/GetVillages/52.71.02
        public async Task<IActionResult> GetVillages(string districtId)
        {
            if (string.IsNullOrEmpty(districtId))
            {
                return BadRequest(new { message = "ID Kecamatan diperlukan." });
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://wilayah.id/api/villages/{districtId}.json";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                return Content(jsonString, "application/json");
            }
            return StatusCode(500, new { message = "Gagal mengambil data kelurahan/desa." });
        }
    }
}