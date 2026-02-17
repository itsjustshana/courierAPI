using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceHistoryController : ControllerBase
    {
         private readonly WarehouseDbContext _context;

        public PriceHistoryController(WarehouseDbContext context)
        {
            _context = context;
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<IEnumerable<PriceHistory>>> GetPriceHistory(int productId)
        {
            var history = await _context.PriceHistories
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.EffectiveDate)
                .ToListAsync();

            if (history == null || history.Count == 0)
            {
                return NotFound();
            }

            return Ok(history);
        }   
    }
}
