using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoriesController : ControllerBase
    {
         private readonly WarehouseDbContext _context;

    public ProductCategoriesController(WarehouseDbContext context)
    {
        _context = context;
    }

   // GET: api/Products
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IEnumerable<ProductCategory>>> GetProducts()
    {
        return await _context.ProductCategories.AsNoTracking().ToListAsync();
    }

    }
}
