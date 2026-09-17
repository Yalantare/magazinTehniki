using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using apibdmagazintehniki.Models;

namespace apibdmagazintehniki.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReceiptsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("get_user_receipts/{userId}")]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetUserReceipts(int userId)
        {
            return await _context.Receipts
                .Where(r => r.UserId == userId)
                .Include(r => r.StatusNavigation)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.Product)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.ProductVariation)
                .ToListAsync();
        }

        [HttpGet("get_cart/{userId}")]
        public async Task<ActionResult<Receipt>> GetCart(int userId)
        {
            var cart = await _context.Receipts
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.Product)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.ProductVariation)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.OrderStatus == 0); 

            if (cart == null)
            {
                cart = new Receipt
                {
                    UserId = userId,
                    TotalPrice = 0,
                    DateTime = DateTime.Now,
                    Status = 1,
                    OrderStatus = 0,
                    Adress = ""
                };
                _context.Receipts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return Ok(cart);
        }

        [HttpPost("add_to_receipts")]
        public async Task<ActionResult> AddToReceipts([FromBody] ReceiptItem receiptItem)
        {
            var product = await _context.Products.FindAsync(receiptItem.ProductId);
            if (product == null)
            {
                return NotFound("Товар не найден.");
            }

            var existingItem = await _context.ReceiptItems
                .FirstOrDefaultAsync(ri => ri.ReceiptId == receiptItem.ReceiptId && ri.ProductId == receiptItem.ProductId && ri.VariationId == receiptItem.VariationId);

            decimal price = product.Price;
            if (receiptItem.VariationId.HasValue)
            {
                var variation = await _context.ProductVariations.FindAsync(receiptItem.VariationId.Value);
                if (variation != null)
                {
                    price = variation.Price;
                }
            }

            if (existingItem != null)
            {
                existingItem.Quantity += receiptItem.Quantity;
                existingItem.PriceAtPurchase = price;
            }
            else
            {
                receiptItem.PriceAtPurchase = price;
                _context.ReceiptItems.Add(receiptItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Товар успешно добавлен" });
        }

        [HttpPut("update_quantity")]
        public async Task<IActionResult> UpdateQuantity(int id, int productId, int quantity, [FromQuery] int? variationId = null)
        {
            if (quantity <= 0)
            {
                return BadRequest("Количество должно быть больше нуля.");
            }

            var receiptItem = await _context.ReceiptItems
                .FirstOrDefaultAsync(ri => ri.ReceiptId == id && ri.ProductId == productId && ri.VariationId == variationId);

            if (receiptItem == null)
            {
                return NotFound("Товар в чеке не найден.");
            }

            receiptItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Количество успешно обновлено." });
        }

        [HttpDelete("delete_item/{receiptId}/{productId}")]
        public async Task<IActionResult> DeleteItem(int receiptId, int productId, [FromQuery] int? variationId = null)
        {
            var receiptItem = await _context.ReceiptItems
                .FirstOrDefaultAsync(ri => ri.ReceiptId == receiptId && ri.ProductId == productId && ri.VariationId == variationId);

            if (receiptItem == null)
            {
                return NotFound("Товар в чеке не найден.");
            }

            _context.ReceiptItems.Remove(receiptItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Товар успешно удален из корзины." });
        }

        public class CheckoutCartDto
        {
            public string? Adress { get; set; }
        }

        [HttpPost("checkout_cart/{receiptId}")]
        public async Task<IActionResult> CheckoutCart(int receiptId, [FromBody] CheckoutCartDto dto)
        {
            var cart = await _context.Receipts
                .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
                .FirstOrDefaultAsync(r => r.ReceiptId == receiptId);

            if (cart == null)
            {
                return NotFound("Корзина не найдена.");
            }

            if (!cart.ReceiptItems.Any())
            {
                return BadRequest("Корзина пуста.");
            }

            decimal total = 0;
            foreach (var item in cart.ReceiptItems)
            {
                decimal price = item.Product!.Price;
                if (item.VariationId.HasValue)
                {
                    var variation = await _context.ProductVariations.FindAsync(item.VariationId.Value);
                    if (variation != null)
                    {
                        price = variation.Price;
                        variation.Stock -= item.Quantity;
                        if (variation.Stock < 0)
                        {
                            variation.Stock = 0;
                        }
                    }
                }
                else if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                    if (item.Product.Stock < 0)
                    {
                        item.Product.Stock = 0;
                    }
                }
                total += price * item.Quantity;
                item.PriceAtPurchase = price; 
            }

            cart.TotalPrice = total;
            cart.OrderStatus = 1;
            cart.DateTime = DateTime.Now;
            cart.Adress = dto.Adress;

            await _context.SaveChangesAsync();

            return Ok(cart);
        }

        [HttpDelete("clear_receipts/{userId}")]
        public async Task<IActionResult> ClearReceipts(int userId)
        {
            var receipts = await _context.Receipts
                .Where(r => r.UserId == userId)
                .ToListAsync();

            if (!receipts.Any())
            {
                return NotFound("Чеки для данного пользователя не найдены.");
            }

            foreach (var receipt in receipts)
            {
                var items = await _context.ReceiptItems
                    .Where(ri => ri.ReceiptId == receipt.ReceiptId)
                    .ToListAsync();

                _context.ReceiptItems.RemoveRange(items);
            }

            _context.Receipts.RemoveRange(receipts);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Все чеки успешно очищены." });
        }

        [HttpPost("checkout/{userId}")]
        public async Task<ActionResult<Receipt>> Checkout(int userId, [FromBody] List<ReceiptItem> items)
        {
            var receipt = new Receipt
            {
                UserId = userId,
                TotalPrice = 0,
                DateTime = DateTime.Now,
                Status = 1,
                OrderStatus = 0,
                Adress = ""
            };

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            decimal total = 0;

            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var receiptItem = new ReceiptItem
                    {
                        ReceiptId = receipt.ReceiptId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PriceAtPurchase = product.Price
                    };

                    total += product.Price * item.Quantity;
                    _context.ReceiptItems.Add(receiptItem);

                    product.Stock -= item.Quantity;
                    if (product.Stock < 0)
                    {
                        product.Stock = 0;
                    }
                }
            }

            receipt.TotalPrice = total;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserReceipts), new { userId = receipt.UserId }, receipt);
        }

        [HttpGet("get_order_history/{userId}")]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetOrderHistory(int userId)
        {
            return await _context.Receipts
                .Where(r => r.UserId == userId)
                .Include(r => r.StatusNavigation)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.Product)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.ProductVariation)
                .ToListAsync();
        }

        [HttpPut("update_order_status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] int statusId)
        {
            var receipt = await _context.Receipts.FindAsync(id);

            if (receipt == null)
            {
                return NotFound("Заказ/чек не найден.");
            }

            receipt.Status = statusId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Статус заказа успешно обновлен." });
        }

        [HttpGet("get_all_receipts")]
        public async Task<ActionResult<IEnumerable<Receipt>>> GetAllReceipts()
        {
            return await _context.Receipts
                .Include(r => r.StatusNavigation)
                .Include(r => r.User)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.Product)
                .Include(r => r.ReceiptItems)
                    .ThenInclude(ri => ri.ProductVariation)
                .ToListAsync();
        }

        [HttpGet("statuses")]
        public async Task<ActionResult<IEnumerable<Status>>> GetStatuses()
        {
            return await _context.Statuses.ToListAsync();
        }
    }
}