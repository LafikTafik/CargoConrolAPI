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
    public class TransportationCompanyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransportationCompanyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/transportationcompany
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _context.TransportationCompany
                .Where(c => !c.IsDeleted)
                .Select(c => new TransportationCompanyDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Address = c.Address,
                    PhoneNumber = c.PhoneNumber
                })
                .ToListAsync();

            return Ok(companies);
        }

        // GET: /api/transportationcompany/deleted
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deletedCompanies = await _context.TransportationCompany
                .Where(c => c.IsDeleted)
                .Select(c => new DeletedTransportationCompanyDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Address = c.Address,
                    PhoneNumber = c.PhoneNumber,
                    IsDeleted = c.IsDeleted,
                    DeletedAt = c.DeletedAt
                })
                .ToListAsync();

            return Ok(deletedCompanies);
        }

        // GET: /api/transportationcompany/{id}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var company = await _context.TransportationCompany
                .Where(c => !c.IsDeleted && c.ID == id)
                .Select(c => new TransportationCompanyDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Address = c.Address,
                    PhoneNumber = c.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (company == null) return NotFound();
            return Ok(company);
        }

        // POST: /api/transportationcompany
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(TransportationCompanyDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Проверим, нет ли уже компании с таким именем
            if (await _context.TransportationCompany.AnyAsync(c => c.Name == dto.Name && !c.IsDeleted))
                return BadRequest("Компания с таким именем уже существует");

            var company = new TransportationCompany
            {
                Name = dto.Name,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                IsDeleted = false
            };

            _context.TransportationCompany.Add(company);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = company.ID }, company);
        }

        // PUT: /api/transportationcompany/{id}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TransportationCompanyDto dto)
        {
            var existing = await _context.TransportationCompany.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();

            // Проверим, не дублируем ли имя
            if (await _context.TransportationCompany.AnyAsync(c => c.Name == dto.Name && c.ID != id && !c.IsDeleted))
                return BadRequest("Компания с таким именем уже существует");

            existing.Name = dto.Name;
            existing.Address = dto.Address;
            existing.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/transportationcompany/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _context.TransportationCompany.FindAsync(id);
            if (company == null || company.IsDeleted) return NotFound();

            company.IsDeleted = true;
            company.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: /api/transportationcompany/restore/{id}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var company = await _context.TransportationCompany.FindAsync(id);
            if (company == null) return NotFound();

            if (!company.IsDeleted)
                return BadRequest("Компания не удалена");

            company.IsDeleted = false;
            company.DeletedAt = null;
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