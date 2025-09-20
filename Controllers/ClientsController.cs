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
    [Authorize] // ← Все методы требуют авторизации
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/clients
        // Только Moderator и Admin могут просматривать всех клиентов
        [Authorize(Roles = "Moderator, Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _context.Clients
                .Where(c => !c.IsDeleted)
                .Select(c => new ClientDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Surname = c.Surname,
                    Phone = c.Phone,
                    Email = c.Email,
                    Adress = c.Adress,
                })
                .ToListAsync();

            return Ok(clients);
        }

        // GET: /api/clients/deleted
        // Только Admin может видеть удалённых
        [Authorize(Roles = "Admin")]
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deletedClients = await _context.Clients
                .Where(c => c.IsDeleted)
                .Select(c => new DeletedClientDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Surname = c.Surname,
                    Phone = c.Phone,
                    Email = c.Email,
                    Adress = c.Adress,
                    IsDeleted = c.IsDeleted,
                    DeletedAt = c.DeletedAt
                })
                .ToListAsync();

            return Ok(deletedClients);
        }

        // GET: /api/clients/{id}
        // User может смотреть только свой профиль, остальные — все
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            // Если User — может смотреть только свой профиль
            if (role == "User")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
                if (user?.ClientID != id)
                    return Forbid("Вы можете просматривать только свой профиль");
            }

            var client = await _context.Clients
                .Where(c => !c.IsDeleted && c.ID == id)
                .Select(c => new ClientDto
                {
                    ID = c.ID,
                    Name = c.Name,
                    Surname = c.Surname,
                    Phone = c.Phone,
                    Email = c.Email,
                    Adress = c.Adress,
                })
                .FirstOrDefaultAsync();

            if (client == null)
                return NotFound();

            return Ok(client);
        }

        // PUT: /api/clients/{id}
        // User может редактировать только свой профиль
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var role = GetUserRole();

            // Проверяем, может ли редактировать
            if (role == "User")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
                if (user?.ClientID != id)
                    return Forbid("Вы можете редактировать только свой профиль");
            }

            var existingClient = await _context.Clients.FindAsync(id);

            if (existingClient == null || existingClient.IsDeleted)
                return NotFound();

            existingClient.Name = dto.Name;
            existingClient.Surname = dto.Surname;
            existingClient.Phone = dto.Phone;
            existingClient.Email = dto.Email;
            existingClient.Adress = dto.Adress;

            _context.Clients.Update(existingClient);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/clients/{id}
        // Только Admin может удалять
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null || client.IsDeleted)
                return NotFound();

            client.IsDeleted = true;
            client.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: /api/clients
        // Только Moderator и Admin могут создавать клиентов
        [Authorize(Roles = "Moderator, Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(ClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var client = new Client
            {
                Name = dto.Name,
                Surname = dto.Surname,
                Phone = dto.Phone,
                Email = dto.Email,
                Adress = dto.Adress,
                IsDeleted = false
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = client.ID }, client);
        }

        // POST: /api/clients/restore/{id}
        // Только Admin может восстанавливать
        [Authorize(Roles = "Admin")]
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
                return NotFound();

            if (!client.IsDeleted)
                return BadRequest("Клиент не удалён");

            client.IsDeleted = false;
            client.DeletedAt = null;

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