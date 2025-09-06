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
    public class TransportationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransportationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/transportations
        // Driver — только свои (через Vehicle), Moderator/Admin — все
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Transportation> query = _context.Transportations.Where(t => !t.IsDeleted);

            if (role == "Driver")
            {
                // Найти DriverID пользователя
                var driverID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.DriverID)
                    .FirstOrDefaultAsync();

                if (driverID == null) return Forbid("Вы не привязаны к водителю");

                // Найти Vehicle, закреплённые за водителем
                var vehicleIDs = _context.Vehicle
                    .Where(v => v.DriverId == driverID)
                    .Select(v => v.ID);

                query = query.Where(t => vehicleIDs.Contains(t.VehicleId));
            }

            var transportations = await query.Select(t => new TransportationDto
            {
                ID = t.ID,
                CargoId = t.CargoID,
                VehicleId = t.VehicleId,
                StartPoint = t.StartPoint,
                EndPoint = t.EndPoint
            }).ToListAsync();

            return Ok(transportations);
        }

        // GET: /api/transportations/deleted
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deleted = await _context.Transportations
                .Where(t => t.IsDeleted)
                .Select(t => new DeletedTransportationDto
                {
                    ID = t.ID,
                    CargoId = t.CargoID,
                    VehicleId = t.VehicleId,
                    StartPoint = t.StartPoint,
                    EndPoint = t.EndPoint,
                    IsDeleted = t.IsDeleted,
                    DeletedAt = t.DeletedAt
                })
                .ToListAsync();

            return Ok(deleted);
        }

        // GET: /api/transportations/{id}
        // Driver — только если транспортировка связана с его транспортом
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var transportation = await _context.Transportations
                .Where(t => !t.IsDeleted && t.ID == id)
                .Select(t => new TransportationDto
                {
                    ID = t.ID,
                    CargoId = t.CargoID,
                    VehicleId = t.VehicleId,
                    StartPoint = t.StartPoint,
                    EndPoint = t.EndPoint
                })
                .FirstOrDefaultAsync();

            if (transportation == null) return NotFound();

            if (role == "Driver")
            {
                var driverID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.DriverID)
                    .FirstOrDefaultAsync();

                if (driverID == null) return Forbid();

                var vehicle = await _context.Vehicle.FindAsync(transportation.VehicleId);
                if (vehicle?.DriverId != driverID)
                    return Forbid("Вы можете просматривать только свои транспортировки");
            }

            return Ok(transportation);
        }

        // POST: /api/transportations
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(TransportationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Проверим, что груз и транспорт существуют
            var cargoExists = await _context.Cargo.AnyAsync(c => c.ID == dto.CargoId && !c.IsDeleted);
            var vehicleExists = await _context.Vehicle.AnyAsync(v => v.ID == dto.VehicleId);

            if (!cargoExists) return BadRequest("Груз не существует");
            if (!vehicleExists) return BadRequest("Транспорт не существует");

            var transportation = new Transportation
            {
                CargoID = dto.CargoId,
                VehicleId = dto.VehicleId,
                StartPoint = dto.StartPoint,
                EndPoint = dto.EndPoint,
                IsDeleted = false
            };

            _context.Transportations.Add(transportation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = transportation.ID }, transportation);
        }

        // PUT: /api/transportations/{id}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TransportationDto dto)
        {
            var existing = await _context.Transportations.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            // Проверим существование
            var cargoExists = await _context.Cargo.AnyAsync(c => c.ID == dto.CargoId && !c.IsDeleted);
            var vehicleExists = await _context.Vehicle.AnyAsync(v => v.ID == dto.VehicleId);

            if (!cargoExists) return BadRequest("Груз не существует");
            if (!vehicleExists) return BadRequest("Транспорт не существует");

            existing.CargoID = dto.CargoId;
            existing.VehicleId = dto.VehicleId;
            existing.StartPoint = dto.StartPoint;
            existing.EndPoint = dto.EndPoint;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/transportations/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var transportation = await _context.Transportations.FindAsync(id);
            if (transportation == null || transportation.IsDeleted) return NotFound();

            transportation.IsDeleted = true;
            transportation.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: /api/transportations/restore/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var transportation = await _context.Transportations.FindAsync(id);
            if (transportation == null) return NotFound();

            if (!transportation.IsDeleted)
                return BadRequest("Транспортировка не удалена");

            transportation.IsDeleted = false;
            transportation.DeletedAt = null;
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