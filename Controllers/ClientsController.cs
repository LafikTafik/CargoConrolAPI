using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted ;
using Microsoft.EntityFrameworkCore;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }
//==================================================================================
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
//==================================================================================
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
//==================================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
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
//==================================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingClient = await _context.Clients.FindAsync(id);

            if (existingClient == null)
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
//==================================================================================
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
//==================================================================================
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
                Adress = dto.Adress
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = client.ID }, client);
        }
//==================================================================================
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
                return NotFound();

            if (!client.IsDeleted)
                return BadRequest("Клиент не удален");

            client.IsDeleted = false;
            client.DeletedAt = null;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}