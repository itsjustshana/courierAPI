using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Models;

[ApiController]
[Route("api/[controller]")] // This maps to 'api/Products'
public class ProductsController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public ProductsController(WarehouseDbContext context)
    {
        _context = context;
    }

   // GET: api/Products
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products.ToListAsync();
    }

    // POST: api/Products
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var historyEntry = new PriceHistory
        {
            ProductId = product.Id,
            PricePerKg = product.CurrentPricePerKg,
            EffectiveDate = DateTime.Now
        };
        _context.PriceHistories.Add(historyEntry);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }

    [HttpPut("{id}/update-price")]
public async Task<IActionResult> UpdatePrice(int id, [FromBody] decimal newPrice)
{
    // 1. Find the product
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    // 2. Update the Product record
    product.CurrentPricePerKg = newPrice;
    product.UpdatedAt = DateTime.Now;

    // 3. Create the History entry (Effective Date tracking)
    var historyEntry = new PriceHistory
    {
        ProductId = id,
        PricePerKg = newPrice,
        EffectiveDate = DateTime.Now
    };

    _context.PriceHistories.Add(historyEntry);

    // 4. Save both changes in one transaction
    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        message = "Price updated and history logged", 
        currentPrice = product.CurrentPricePerKg,
        effectiveDate = historyEntry.EffectiveDate 
    });
}


[HttpDelete("{id}")] // This allows the /34 at the end of the URL
public async Task<IActionResult> DeleteProduct(int id)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null)
    {
        return NotFound();
    }

    _context.Products.Remove(product);
    await _context.SaveChangesAsync();

    return NoContent();
}

[HttpPost("{id}/update-image")] // Using a fixed string "update-image" avoids conflicts
public async Task<IActionResult> UpdateProductImage(int id, [FromBody] string img)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    product.Image = img;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Image updated successfully", image = img });
}

//out of stock vs in stock
[HttpPut("{id}/update-availability")]
public async Task<IActionResult> UpdateProductAvailability(int id, [FromBody] bool isAvailable)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    product.IsAvailable = isAvailable;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Availability updated successfully", isAvailable = isAvailable });    
}

//active vs inactive
[HttpPut("{id}/update-active")]
public async Task<IActionResult> UpdateProductActive(int id, [FromBody] bool isActive)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    product.IsActive = isActive;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Active status updated successfully", isActive = isActive });    
}
[HttpPut("{id}/{date}/update-price")]
public async Task<IActionResult> UpdatePriceWithEffectiveDate(int id, DateTime date, [FromBody] decimal newPrice)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    // Update the Product record
    product.CurrentPricePerKg = newPrice;
    product.UpdatedAt = DateTime.Now;

    // Create the History entry with the provided effective date
    var historyEntry = new PriceHistory
    {
        ProductId = id,
        PricePerKg = newPrice,
        EffectiveDate = date
    };

    _context.PriceHistories.Add(historyEntry);
    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        message = "Price updated with effective date", 
        currentPrice = product.CurrentPricePerKg,
        effectiveDate = historyEntry.EffectiveDate 
    });
}


}

