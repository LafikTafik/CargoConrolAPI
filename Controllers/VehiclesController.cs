using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using CCAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _context;

    public VehiclesController(AppDbContext context)
    {
        _context = context;
    }
    //==================================================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _context.Vehicle
            .Where(v => !v.IsDeleted)
            .Select(v => new VehicleDto
            {
                ID = v.ID,
                TransportationCompanyId = v.TransportationCompanyId,
                Type = v.Type,
                Capacity = v.Capacity,
                DriverId = v.DriverId,
                VehicleNum = v.VehicleNum
            })
            .ToListAsync();

        return Ok(vehicles);
    }
    //==================================================================================
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
    //==================================================================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
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
        return Ok(vehicle);
    }
    //==================================================================================
    [HttpPost]
    public async Task<IActionResult> Create(VehicleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var vehicle = new Vehicle
        {
            TransportationCompanyId = dto.TransportationCompanyId,
            Type = dto.Type,
            Capacity = dto.Capacity,
            DriverId = dto.DriverId,
            VehicleNum = dto.VehicleNum
        };

        _context.Vehicle.Add(vehicle);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = vehicle.ID }, vehicle);
    }
    //==================================================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, VehicleDto dto)
    {
        var existing = await _context.Vehicle.FindAsync(id);
        if (existing == null || existing.IsDeleted) return NotFound();

        existing.TransportationCompanyId = dto.TransportationCompanyId;
        existing.Type = dto.Type;
        existing.Capacity = dto.Capacity;
        existing.DriverId = dto.DriverId;
        existing.VehicleNum = dto.VehicleNum;

        await _context.SaveChangesAsync();
        return NoContent();
    }
    //==================================================================================
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
    //==================================================================================
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> Restore(int id)
    {
        var vehicle = await _context.Vehicle.FindAsync(id);
        if (vehicle == null) return NotFound();

        vehicle.IsDeleted = false;
        vehicle.DeletedAt = null;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}