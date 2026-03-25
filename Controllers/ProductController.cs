using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Hubs;
using WarehouseApi.Models;

[ApiController]
[Route("api/[controller]")] // This maps to 'api/Products'
public class ProductsController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly IHubContext<PriceHub> _hubContext;


    public ProductsController(WarehouseDbContext context, IHubContext<PriceHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
        
    }

   // GET: api/Products
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products.AsNoTracking().ToListAsync();
    }

    // POST: api/Products
[HttpPost]
public async Task<ActionResult<Product>> PostProduct(ProductCreateDto dto)
{
    // Access the items via the DTO
    var product = dto.Product;
    
    _context.Products.Add(product!);
    await _context.SaveChangesAsync();

    var historyEntry = new PriceHistory
    {
        ProductId = product!.Id,
        PricePerKg = product.CurrentPricePerKg,
        EffectiveDate = DateTime.Now,
        UpdatedBy = dto.UpdatedBy // Use the DTO value
    };

    _context.PriceHistories.Add(historyEntry);
    await _context.SaveChangesAsync();
    

if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
    return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
}

 [HttpPut("{id}/update-price")]
public async Task<IActionResult> UpdatePrice(int id, [FromBody] PriceUpdateDto data)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    // Update the Product record
    product.CurrentPricePerKg = data.NewPrice;
    product.UpdatedAt = DateTime.Now;

    // Create the History entry
    var historyEntry = new PriceHistory
    {
        ProductId = id,
        PricePerKg = data.NewPrice,
        EffectiveDate = data.EffectiveDate,
        UpdatedBy = data.UpdatedBy
    };

    _context.PriceHistories.Add(historyEntry);
    await _context.SaveChangesAsync();

if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
    return Ok(new { message = "Update Successful", effectiveDate = historyEntry.EffectiveDate });
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
if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
    return NoContent();
}

[HttpPost("{id}/update-image")] // Using a fixed string "update-image" avoids conflicts
public async Task<IActionResult> UpdateProductImage(int id, [FromBody] string img)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();

    product.Image = img;
    await _context.SaveChangesAsync();
if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
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
if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
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
if (_hubContext != null)
{
    Console.WriteLine($"---> SIGNALR: Broadcasting 'RefreshPriceBoard' from {Request.Method} {Request.Path}");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
    return Ok(new { message = "Active status updated successfully", isActive = isActive });    
}
[HttpPut("{id}/{date}/{updatedby}/update-price")]
public async Task<IActionResult> UpdatePriceWithEffectiveDate(
    [FromRoute] int id, 
    [FromRoute] DateTime date, 
    [FromRoute] string updatedby, 
    [FromBody] decimal newPrice)
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
        EffectiveDate = date,
        UpdatedBy = updatedby
    };

    _context.PriceHistories.Add(historyEntry);
    await _context.SaveChangesAsync();
if (_hubContext != null)
{
    Console.WriteLine("---> SIGNALR: Sending RefreshPriceBoard alert now!");
    await _hubContext.Clients.All.SendAsync("RefreshPriceBoard");
}
else 
{
    Console.WriteLine("---> SIGNALR ERROR: HubContext is NULL!");
}
    return Ok(new 
    { 
        message = "Price updated with effective date", 
        currentPrice = product.CurrentPricePerKg,
        effectiveDate = historyEntry.EffectiveDate 
    });
}

public class ProductCreateDto
{
    public Product? Product { get; set; }
    public string? UpdatedBy { get; set; }
}

}

