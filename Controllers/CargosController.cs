using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using Microsoft.EntityFrameworkCore;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CargosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CargosController(AppDbContext context)
        {
            _context = context;
        }
        //==================================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cargos = await _context.Cargo
                .Where(c => !c.IsDeleted)
                .Select(c => new CargoDto
                {
                    ID = c.ID,
                    Weight = c.Weight,
                    Dimensions = c.Dimensions,
                    Descriptions = c.Descriptions
                })
                .ToListAsync();

            return Ok(cargos);
        }
        //==================================================================================
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
        //==================================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
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
            return Ok(cargo);
        }
        //==================================================================================
        [HttpPost]
        public async Task<IActionResult> Create(CargoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cargo = new Cargos
            {
                Weight = dto.Weight,
                Dimensions = dto.Dimensions,
                Descriptions = dto.Descriptions
            };

            _context.Cargo.Add(cargo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = cargo.ID }, cargo);
        }
        //==================================================================================
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
        //==================================================================================
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
        //==================================================================================
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var cargo = await _context.Cargo.FindAsync(id);
            if (cargo == null) return NotFound();

            cargo.IsDeleted = false;
            cargo.DeletedAt = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}