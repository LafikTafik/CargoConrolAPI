using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using CCAPI.DTO.deleted;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TransportationCompanyController : ControllerBase
{
    private readonly AppDbContext _context;

    public TransportationCompanyController(AppDbContext context)
    {
        _context = context;
    }

    //==================================================================================
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
    //==================================================================================
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
    //==================================================================================
    [HttpPost]
    public async Task<IActionResult> Create(TransportationCompanyDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var company = new TransportationCompany
        {
            Name = dto.Name,
            Address = dto.Address,
            PhoneNumber = dto.PhoneNumber
        };

        _context.TransportationCompany.Add(company);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = company.ID }, company);
    }
    //==================================================================================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TransportationCompanyDto dto)
    {
        var existing = await _context.TransportationCompany.FindAsync(id);
        if (existing == null || existing.IsDeleted) return NotFound();

        existing.Name = dto.Name;
        existing.Address = dto.Address;
        existing.PhoneNumber = dto.PhoneNumber;

        await _context.SaveChangesAsync();
        return NoContent();
    }
    //==================================================================================
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
    //==================================================================================
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
    //==================================================================================
    [HttpPost("restore/{id}")]
    public async Task<IActionResult> Restore(int id)
    {
        var company = await _context.TransportationCompany.FindAsync(id);
        if (company == null) return NotFound();

        company.IsDeleted = false;
        company.DeletedAt = null;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}