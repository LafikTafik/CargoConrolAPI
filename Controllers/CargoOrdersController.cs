using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все методы требуют авторизации
    public class CargoOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CargoOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/cargoorders/{orderId}
        // User — только если заказ его, Moderator/Admin — все
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            // Проверка, что заказ принадлежит пользователю (если User)
            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid("Вы не привязаны к клиенту");

                var order = await _context.Order.FindAsync(orderId);
                if (order == null || order.IDClient != clientID)
                    return Forbid("Вы можете просматривать только свои заказы");
            }

            var items = await _context.CargoOrders
                .Where(co => co.OrderID == orderId)
                .Select(co => new CargoOrdersDto
                {
                    OrderID = co.OrderID,
                    CargoID = co.CargoID
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/cargoorders
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var links = await _context.CargoOrders
                .Select(co => new CargoOrdersDto
                {
                    CargoID = co.CargoID,
                    OrderID = co.OrderID
                })
                .ToListAsync();

            return Ok(links);
        }

        // POST: /api/cargoorders
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CargoOrdersDto dto)
        {
            // Проверим, что заказ и груз существуют
            var orderExists = await _context.Order.AnyAsync(o => o.ID == dto.OrderID && !o.IsDeleted);
            var cargoExists = await _context.Cargo.AnyAsync(c => c.ID == dto.CargoID && !c.IsDeleted);

            if (!orderExists) return BadRequest("Заказ не существует");
            if (!cargoExists) return BadRequest("Груз не существует");

            // Проверим, нет ли уже такой связи
            var exists = await _context.CargoOrders
                .AnyAsync(co => co.OrderID == dto.OrderID && co.CargoID == dto.CargoID);

            if (exists)
                return BadRequest("Связь между грузом и заказом уже существует");

            var cargoOrder = new CargoOrders
            {
                OrderID = dto.OrderID,
                CargoID = dto.CargoID
            };

            _context.CargoOrders.Add(cargoOrder);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: /api/cargoorders/{cargoId}/{orderId}
        // User — только если заказ его
        [HttpGet("{cargoId}/{orderId}")]
        public async Task<IActionResult> GetByCompositeKey(int cargoId, int orderId)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid();

                var order = await _context.Order.FindAsync(orderId);
                if (order?.IDClient != clientID)
                    return Forbid("Вы можете просматривать только свои заказы");
            }

            var link = await _context.CargoOrders
                .Where(co => co.CargoID == cargoId && co.OrderID == orderId)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            var dto = new CargoOrdersDto
            {
                CargoID = link.CargoID,
                OrderID = link.OrderID
            };

            return Ok(dto);
        }

        // DELETE: /api/cargoorders/{cargoId}/{orderId}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpDelete("{cargoId}/{orderId}")]
        public async Task<IActionResult> Delete(int cargoId, int orderId)
        {
            var link = await _context.CargoOrders
                .Where(co => co.CargoID == cargoId && co.OrderID == orderId)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            _context.CargoOrders.Remove(link);
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