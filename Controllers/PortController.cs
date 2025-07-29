using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PerizinanPeternakan.Controllers
{
    public class PortController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PortController> _logger;

        public PortController(ApplicationDbContext context, ILogger<PortController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            try
            {
                var query = _context.Ports
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(p =>
                        p.Name.Contains(term) ||
                        p.City.Contains(term) ||
                        p.Province.Contains(term)
                    );
                }

                var ports = await query
                    .OrderBy(p => p.Name)
                    .Take(20)
                    .Select(p => new
                    {
                        id = p.Name,
                        text = $"{p.Name} ({p.City}, {p.Province})"
                    })
                    .ToListAsync();

                return Json(new { results = ports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ports with term: {Term}", term);
                return Json(new { results = new List<object>() });
            }
        }

       
        [HttpGet]
        public async Task<IActionResult> GetByProvince(string provinceCode)
        {
            try
            {
                if (string.IsNullOrEmpty(provinceCode))
                {
                    return BadRequest(new { message = "Province code is required" });
                }

                var ports = await _context.Ports
                    .Where(p => p.ProvinceCode == provinceCode && p.IsActive)
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.Name,
                        text = $"{p.Name} ({p.City})",
                        code = p.Code,
                        name = p.Name,
                        city = p.City,
                        province = p.Province,
                        type = p.Type
                    })
                    .ToListAsync();

                return Json(new
                {
                    results = ports,
                    meta = new
                    {
                        total = ports.Count,
                        provinceCode = provinceCode
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ports for province: {ProvinceCode}", provinceCode);
                return StatusCode(500, new { message = "Error retrieving ports" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var ports = await _context.Ports
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Province)
                    .ThenBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.Id,
                        code = p.Code,
                        name = p.Name,
                        city = p.City,
                        province = p.Province,
                        provinceCode = p.ProvinceCode,
                        type = p.Type
                    })
                    .ToListAsync();

                return Json(new
                {
                    data = ports,
                    meta = new
                    {
                        total = ports.Count,
                        updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all ports");
                return StatusCode(500, new { message = "Error retrieving all ports" });
            }
        }

        /// <summary>
        /// Get ports grouped by province - untuk keperluan statistik
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGroupedByProvince()
        {
            try
            {
                var groupedPorts = await _context.Ports
                    .Where(p => p.IsActive)
                    .GroupBy(p => new { p.Province, p.ProvinceCode })
                    .Select(g => new
                    {
                        province = g.Key.Province,
                        provinceCode = g.Key.ProvinceCode,
                        count = g.Count(),
                        ports = g.Select(p => new
                        {
                            name = p.Name,
                            city = p.City,
                            type = p.Type
                        }).OrderBy(p => p.name).ToList()
                    })
                    .OrderBy(g => g.province)
                    .ToListAsync();

                return Json(new
                {
                    data = groupedPorts,
                    meta = new
                    {
                        totalProvinces = groupedPorts.Count,
                        totalPorts = groupedPorts.Sum(g => g.count)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grouped ports");
                return StatusCode(500, new { message = "Error retrieving grouped ports" });
            }
        }

        /// <summary>
        /// Get port statistics - untuk dashboard admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _context.Ports
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.Type)
                    .Select(g => new
                    {
                        type = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var totalPorts = await _context.Ports.CountAsync(p => p.IsActive);
                var totalProvinces = await _context.Ports
                    .Where(p => p.IsActive)
                    .Select(p => p.ProvinceCode)
                    .Distinct()
                    .CountAsync();

                return Json(new
                {
                    data = new
                    {
                        totalPorts = totalPorts,
                        totalProvinces = totalProvinces,
                        byType = stats
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting port statistics");
                return StatusCode(500, new { message = "Error retrieving port statistics" });
            }
        }
    }
}