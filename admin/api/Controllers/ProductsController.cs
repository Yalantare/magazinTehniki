using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using apibdmagazintehniki.Models;

namespace apibdmagazintehniki.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Categorye>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categoryes.AsNoTracking().ToListAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] int? categoryId)
        {
            try
            {
                
                var query = _context.Products
                    .Include(p => p.CategoryNavigation)
                    .Include(p => p.ProductVariations)
                    .AsNoTracking()
                    .AsQueryable();

                
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.Category == categoryId.Value);
                }

                
                var products = await query.ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProducts), new { id = product.Articul }, product);
        }

        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int stock)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Продукт не найден.");
            }

            product.Stock = stock;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Остаток успешно обновлен" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Articul)
            {
                return BadRequest("Артикул продукта не совпадает.");
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Articul == id))
                {
                    return NotFound("Продукт не найден.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Продукт не найден.");
            }

            
            var variations = _context.ProductVariations.Where(v => v.ProductId == id);
            _context.ProductVariations.RemoveRange(variations);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/variations")]
        public async Task<ActionResult<IEnumerable<ProductVariation>>> GetProductVariations(int id)
        {
            return await _context.ProductVariations.Where(v => v.ProductId == id).ToListAsync();
        }

        [HttpPost("variations")]
        public async Task<ActionResult<ProductVariation>> PostProductVariation(ProductVariation variation)
        {
            _context.ProductVariations.Add(variation);
            await _context.SaveChangesAsync();
            return Ok(variation);
        }

        [HttpDelete("variations/{id}")]
        public async Task<IActionResult> DeleteProductVariation(int id)
        {
            var variation = await _context.ProductVariations.FindAsync(id);
            if (variation == null) return NotFound();
            _context.ProductVariations.Remove(variation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}