using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using Microsoft.EntityFrameworkCore;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }
        //==================================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Order
                .Where(o => !o.IsDeleted)
                .Select(o => new OrderDto
                {
                    ID = o.ID,
                    TransId = o.TransId,
                    IDClient = o.IDClient,
                    Date = o.Date,
                    Status = o.Status,
                    Price = o.Price
                })
                .ToListAsync();

            return Ok(orders);
        }
        //==================================================================================
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deletedOrders = await _context.Order
                .Where(o => o.IsDeleted)
                .Select(o => new DeletedOrderDto
                {
                    ID = o.ID,
                    TransId = o.TransId,
                    IDClient = o.IDClient,
                    Date = o.Date,
                    Status = o.Status,
                    Price = o.Price,
                    IsDeleted = o.IsDeleted,
                    DeletedAt = o.DeletedAt
                })
                .ToListAsync();

            return Ok(deletedOrders);
        }
        //==================================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _context.Order
                .Where(o => !o.IsDeleted && o.ID == id)
                .Select(o => new OrderDto
                {
                    ID = o.ID,
                    TransId = o.TransId,
                    IDClient = o.IDClient,
                    Date = o.Date,
                    Status = o.Status,
                    Price = o.Price
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();
            return Ok(order);
        }
        //==================================================================================
        [HttpPost]
        public async Task<IActionResult> Create(OrderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = new Orders
            {
                TransId = dto.TransId,
                IDClient = dto.IDClient,
                Date = dto.Date,
                Status = dto.Status,
                Price = dto.Price
            };

            _context.Order.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = order.ID }, order);
        }
        //==================================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, OrderDto dto)
        {
            var existing = await _context.Order.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.TransId = dto.TransId;
            existing.IDClient = dto.IDClient;
            existing.Date = dto.Date;
            existing.Status = dto.Status;
            existing.Price = dto.Price;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        //==================================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null || order.IsDeleted) return NotFound();

            order.IsDeleted = true;
            order.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        //==================================================================================
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null) return NotFound();

            order.IsDeleted = false;
            order.DeletedAt = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}