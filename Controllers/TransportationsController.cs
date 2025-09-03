using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
public class TransportationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TransportationsController(AppDbContext context)
    {
        _context = context;
    }
    //==================================================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var transportations = await _context.Transportations
            .Where(t => !t.IsDeleted)
            .Select(t => new TransportationDto
            {
                ID = t.ID,
                CargoId = t.CargoID,
                VehicleId = t.VehicleId,
                StartPoint = t.StartPoint,
                EndPoint = t.EndPoint
            })
            .ToListAsync();

        return Ok(transportations);
    }
    //==================================================================================
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
    //==================================================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
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
        return Ok(transportation);
    }
    //==================================================================================
    [HttpPost]
    public async Task<IActionResult> Create(TransportationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var transportation = new Transportation
        {
            CargoID = dto.CargoId,
            VehicleId = dto.VehicleId,
            StartPoint = dto.StartPoint,
            EndPoint = dto.EndPoint
        };

        _context.Transportations.Add(transportation);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = transportation.ID }, transportation);
    }
    //==================================================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TransportationDto dto)
    {
        var existing = await _context.Transportations.FindAsync(id);
        if (existing == null || existing.IsDeleted) return NotFound();

        existing.CargoID = dto.CargoId;
        existing.VehicleId = dto.VehicleId;
        existing.StartPoint = dto.StartPoint;
        existing.EndPoint = dto.EndPoint;

        await _context.SaveChangesAsync();
        return NoContent();
    }
    //==================================================================================
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
    //==================================================================================
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> Restore(int id)
    {
        var transportation = await _context.Transportations.FindAsync(id);
        if (transportation == null) return NotFound();

        transportation.IsDeleted = false;
        transportation.DeletedAt = null;

        await _context.SaveChangesAsync();
        return NoContent();
    }

}