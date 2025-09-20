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
    public class DriversController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DriversController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/drivers
        // Только Moderator и Admin могут просматривать всех водителей
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var drivers = await _context.Drivers
                .Where(d => !d.IsDeleted)
                .Select(d => new DriverDto
                {
                    ID = d.ID,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber
                })
                .ToListAsync();

            return Ok(drivers);
        }

        // GET: /api/drivers/deleted
        // Только Admin может видеть удалённых
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deleted = await _context.Drivers
                .Where(d => d.IsDeleted)
                .Select(d => new DeletedDriverDto
                {
                    ID = d.ID,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber,
                    IsDeleted = d.IsDeleted,
                    DeletedAt = d.DeletedAt
                })
                .ToListAsync();

            return Ok(deleted);
        }

        // GET: /api/drivers/{id}
        // Driver может смотреть только свой профиль
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (role == "Driver")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
                if (user?.DriverID != id)
                    return Forbid("Вы можете просматривать только свой профиль");
            }

            var driver = await _context.Drivers
                .Where(d => !d.IsDeleted && d.ID == id)
                .Select(d => new DriverDto
                {
                    ID = d.ID,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (driver == null) return NotFound();
            return Ok(driver);
        }

        // POST: /api/drivers
        // Только Moderator и Admin могут создавать водителей
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(DriverDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var driver = new Driver
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                LicenseNumber = dto.LicenseNumber,
                PhoneNumber = dto.PhoneNumber,
                IsDeleted = false
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = driver.ID }, driver);
        }

        // PUT: /api/drivers/{id}
        // Driver может редактировать только свой профиль
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DriverDto dto)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (role == "Driver")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
                if (user?.DriverID != id)
                    return Forbid("Вы можете редактировать только свой профиль");
            }

            var existing = await _context.Drivers.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            existing.FirstName = dto.FirstName;
            existing.LastName = dto.LastName;
            existing.LicenseNumber = dto.LicenseNumber;
            existing.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/drivers/{id}
        // Только Admin может удалять
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null || driver.IsDeleted) return NotFound();

            driver.IsDeleted = true;
            driver.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: /api/drivers/restore/{id}
        // Только Admin может восстанавливать
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            if (!driver.IsDeleted)
                return BadRequest("Водитель не удалён");

            driver.IsDeleted = false;
            driver.DeletedAt = null;

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