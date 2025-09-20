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
    public class TransCompController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransCompController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/transcomp
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var links = await _context.TransComp
                .Select(tc => new TransCompDto
                {
                    TransportationID = tc.TransportationID,
                    CompanyID = tc.CompanyID
                })
                .ToListAsync();

            return Ok(links);
        }

        // GET: /api/transcomp/{transportationId}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet("{transportationId}")]
        public async Task<IActionResult> GetByTransportationId(int transportationId)
        {
            var items = await _context.TransComp
                .Where(tc => tc.TransportationID == transportationId)
                .Select(tc => new TransCompDto
                {
                    TransportationID = tc.TransportationID,
                    CompanyID = tc.CompanyID
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/transcomp/{transid}/{companyid}
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet("{transid}/{companyid}")]
        public async Task<IActionResult> GetByCompositeKey(int transid, int companyid)
        {
            var link = await _context.TransComp
                .Where(tc => tc.TransportationID == transid && tc.CompanyID == companyid)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            return Ok(new TransCompDto
            {
                TransportationID = link.TransportationID,
                CompanyID = link.CompanyID
            });
        }

        // POST: /api/transcomp
        // Только Moderator и Admin
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(TransCompDto dto)
        {
            // Проверим, что транспортировка и компания существуют
            var transExists = await _context.TransportationCompany.AnyAsync(t => t.ID == dto.TransportationID);
            var compExists = await _context.TransportationCompany.AnyAsync(c => c.ID == dto.CompanyID);

            if (!transExists) return BadRequest("Транспортировка не существует");
            if (!compExists) return BadRequest("Компания не существует");

            // Проверим, нет ли уже такой связи
            var exists = await _context.TransComp
                .AnyAsync(tc => tc.TransportationID == dto.TransportationID && tc.CompanyID == dto.CompanyID);

            if (exists)
                return BadRequest("Связь между транспортировкой и компанией уже существует");

            var transComp = new TransComp
            {
                TransportationID = dto.TransportationID,
                CompanyID = dto.CompanyID
            };

            _context.TransComp.Add(transComp);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/transcomp/{transId}/{companyId}
        // Только Admin
        [Authorize(Roles = "Admin")]
        [HttpDelete("{transId}/{companyId}")]
        public async Task<IActionResult> Delete(int transId, int companyId)
        {
            var link = await _context.TransComp
                .Where(tc => tc.TransportationID == transId && tc.CompanyID == companyId)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            _context.TransComp.Remove(link);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Вспомогательные методы (на случай, если понадобятся)
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