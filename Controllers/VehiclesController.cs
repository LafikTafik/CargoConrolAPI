using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using CCAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Все методы требуют авторизации
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiclesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/vehicles
        // Driver — только свои, Moderator/Admin — все
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Vehicle> query = _context.Vehicle.Where(v => !v.IsDeleted);

            if (role == "Driver")
            {
                var driverID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.DriverID)
                    .FirstOrDefaultAsync();

                if (driverID == null) return Forbid("Вы не привязаны к водителю");

                query = query.Where(v => v.DriverId == driverID);
            }

            var vehicles = await query.Select(v => new VehicleDto
            {
                ID = v.ID,
                TransportationCompanyId = v.TransportationCompanyId,
                Type = v.Type,
                Capacity = v.Capacity,
                DriverId = v.DriverId,
                VehicleNum = v.VehicleNum
            }).ToListAsync();

            return Ok(vehicles);
        }

        // GET: /api/vehicles/deleted
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deleted = await _context.Vehicle
                .Where(v => v.IsDeleted)
                .Select(v => new DeletedVehicleDto
                {
                    ID = v.ID,
                    TransportationCompanyId = v.TransportationCompanyId,
                    Type = v.Type,
                    Capacity = v.Capacity,
                    DriverId = v.DriverId,
                    VehicleNum = v.VehicleNum,
                    IsDeleted = v.IsDeleted,
                    DeletedAt = v.DeletedAt
                })
                .ToListAsync();

            return Ok(deleted);
        }

        // GET: /api/vehicles/{id}
        // Driver — только свой транспорт
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var vehicle = await _context.Vehicle
                .Where(v => !v.IsDeleted && v.ID == id)
                .Select(v => new VehicleDto
                {
                    ID = v.ID,
                    TransportationCompanyId = v.TransportationCompanyId,
                    Type = v.Type,
                    Capacity = v.Capacity,
                    DriverId = v.DriverId,
                    VehicleNum = v.VehicleNum
                })
                .FirstOrDefaultAsync();

            if (vehicle == null) return NotFound();

            if (role == "Driver")
            {
                var driverID = await _context.Users
                    .Where(u => u.ID == userId)
                    .Select(u => u.DriverID)
                    .FirstOrDefaultAsync();

                if (vehicle.DriverId != driverID)
                    return Forbid("Вы можете просматривать только свой транспорт");
            }

            return Ok(vehicle);
        }

        // POST: /api/vehicles
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(VehicleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Проверим, что компания существует
            var companyExists = await _context.TransportationCompany.AnyAsync(c => c.ID == dto.TransportationCompanyId);
            if (!companyExists)
                return BadRequest("Транспортная компания не существует");

            // Проверим, что водитель существует (если указан)
            if (dto.DriverId.HasValue)
            {
                var driverExists = await _context.Drivers.AnyAsync(d => d.ID == dto.DriverId.Value && !d.IsDeleted);
                if (!driverExists)
                    return BadRequest("Водитель не существует");
            }

            var vehicle = new Vehicle
            {
                TransportationCompanyId = dto.TransportationCompanyId,
                Type = dto.Type,
                Capacity = dto.Capacity,
                DriverId = (int)dto.DriverId,
                VehicleNum = dto.VehicleNum,
                IsDeleted = false
            };

            _context.Vehicle.Add(vehicle);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = vehicle.ID }, vehicle);
        }

        // PUT: /api/vehicles/{id}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, VehicleDto dto)
        {
            var existing = await _context.Vehicle.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            // Проверим существование
            var companyExists = await _context.TransportationCompany.AnyAsync(c => c.ID == dto.TransportationCompanyId);
            if (!companyExists)
                return BadRequest("Транспортная компания не существует");

            if (dto.DriverId.HasValue)
            {
                var driverExists = await _context.Drivers.AnyAsync(d => d.ID == dto.DriverId.Value && !d.IsDeleted);
                if (!driverExists)
                    return BadRequest("Водитель не существует");
            }

            existing.TransportationCompanyId = dto.TransportationCompanyId;
            existing.Type = dto.Type;
            existing.Capacity = dto.Capacity;
            existing.DriverId = (int)dto.DriverId;
            existing.VehicleNum = dto.VehicleNum;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/vehicles/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _context.Vehicle.FindAsync(id);
            if (vehicle == null || vehicle.IsDeleted) return NotFound();

            vehicle.IsDeleted = true;
            vehicle.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: /api/vehicles/restore/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var vehicle = await _context.Vehicle.FindAsync(id);
            if (vehicle == null) return NotFound();

            if (!vehicle.IsDeleted)
                return BadRequest("Транспорт не удалён");

            vehicle.IsDeleted = false;
            vehicle.DeletedAt = null;
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