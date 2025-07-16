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
    }
}