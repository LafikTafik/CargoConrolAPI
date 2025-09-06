using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все методы требуют авторизации
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/orders
        // User — только свои, Moderator и Admin — все
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Orders> query = _context.Order.Where(o => !o.IsDeleted);

            if (role == "User")
            {
                // Найти ClientID пользователя
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid("Вы не привязаны к клиенту");

                query = query.Where(o => o.IDClient == clientID);
            }

            var orders = await query.Select(o => new OrderDto
            {
                ID = o.ID,
                TransId = o.TransId,
                IDClient = o.IDClient,
                Date = o.Date,
                Status = o.Status,
                Price = o.Price
            }).ToListAsync();

            return Ok(orders);
        }

        // GET: /api/orders/deleted
        // Только Admin
        [Authorize(Roles = "Admin")]
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

        // GET: /api/orders/{id}
        // User — только свой заказ
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

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

            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (order.IDClient != clientID)
                    return Forbid("Вы можете просматривать только свои заказы");
            }

            return Ok(order);
        }

        // POST: /api/orders
        // User — только для своего ClientID, Moderator и Admin — любые
        [HttpPost]
        public async Task<IActionResult> Create(OrderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var role = GetUserRole();

            // Проверка, что User создаёт заказ только для себя
            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid("Вы не привязаны к клиенту");

                if (dto.IDClient != clientID)
                    return Forbid("Вы можете создавать заказы только для себя");
            }

            var order = new Orders
            {
                TransId = dto.TransId,
                IDClient = dto.IDClient,
                Date = dto.Date,
                Status = dto.Status,
                Price = dto.Price,
                IsDeleted = false
            };

            _context.Order.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = order.ID }, order);
        }

        // PUT: /api/orders/{id}
        // Только Moderator и Admin могут редактировать
        [Authorize(Roles = "Moderator, Admin")]
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

        // DELETE: /api/orders/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
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

        // POST: /api/orders/restore/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null) return NotFound();

            if (!order.IsDeleted)
                return BadRequest("Заказ не удалён");

            order.IsDeleted = false;
            order.DeletedAt = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Вспомогательные методы
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";
        }
    }
}