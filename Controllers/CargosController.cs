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
    public class CargosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CargosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/cargos
        // User — только грузы в своих заказах, Moderator/Admin — все
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Cargos> query = _context.Cargo.Where(c => !c.IsDeleted);

            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid("Вы не привязаны к клиенту");

                // Грузы, которые есть в заказах этого клиента
                var orderIDs = _context.Order
                    .Where(o => o.IDClient == clientID && !o.IsDeleted)
                    .Select(o => o.ID);

                var cargoIDs = _context.CargoOrders
                    .Where(co => orderIDs.Contains(co.OrderID))
                    .Select(co => co.CargoID);

                query = query.Where(c => cargoIDs.Contains(c.ID));
            }

            var cargos = await query.Select(c => new CargoDto
            {
                ID = c.ID,
                Weight = c.Weight,
                Dimensions = c.Dimensions,
                Descriptions = c.Descriptions
            }).ToListAsync();

            return Ok(cargos);
        }

        // GET: /api/cargos/deleted
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deletedCargos = await _context.Cargo
                .Where(c => c.IsDeleted)
                .Select(c => new DeletedCargoDto
                {
                    ID = c.ID,
                    Weight = c.Weight,
                    Dimensions = c.Dimensions,
                    Descriptions = c.Descriptions,
                    IsDeleted = c.IsDeleted,
                    DeletedAt = c.DeletedAt
                })
                .ToListAsync();

            return Ok(deletedCargos);
        }

        // GET: /api/cargos/{id}
        // Проверка, что User имеет доступ
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var cargo = await _context.Cargo
                .Where(c => !c.IsDeleted && c.ID == id)
                .Select(c => new CargoDto
                {
                    ID = c.ID,
                    Weight = c.Weight,
                    Dimensions = c.Dimensions,
                    Descriptions = c.Descriptions
                })
                .FirstOrDefaultAsync();

            if (cargo == null) return NotFound();

            if (role == "User")
            {
                var clientID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.ClientID)
                    .FirstOrDefaultAsync();

                if (clientID == null) return Forbid();

                // Проверяем, есть ли этот груз в заказе клиента
                var orderID = await _context.CargoOrders
                    .Where(co => co.CargoID == id)
                    .Select(co => co.OrderID)
                    .FirstOrDefaultAsync();

                if (orderID == 0) return Forbid("Груз не найден");

                var order = await _context.Order.FindAsync(orderID);
                if (order?.IDClient != clientID)
                    return Forbid("Вы можете просматривать только свои грузы");
            }

            return Ok(cargo);
        }

        // POST: /api/cargos
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CargoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cargo = new Cargos
            {
                Weight = dto.Weight,
                Dimensions = dto.Dimensions,
                Descriptions = dto.Descriptions,
                IsDeleted = false
            };

            _context.Cargo.Add(cargo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = cargo.ID }, cargo);
        }

        // PUT: /api/cargos/{id}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CargoDto dto)
        {
            var existing = await _context.Cargo.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.Weight = dto.Weight;
            existing.Dimensions = dto.Dimensions;
            existing.Descriptions = dto.Descriptions;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/cargos/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cargo = await _context.Cargo.FindAsync(id);
            if (cargo == null || cargo.IsDeleted) return NotFound();

            cargo.IsDeleted = true;
            cargo.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: /api/cargos/restore/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var cargo = await _context.Cargo.FindAsync(id);
            if (cargo == null) return NotFound();

            if (!cargo.IsDeleted)
                return BadRequest("Груз не удалён");

            cargo.IsDeleted = false;
            cargo.DeletedAt = null;
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