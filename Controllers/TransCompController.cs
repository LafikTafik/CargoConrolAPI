using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using Microsoft.EntityFrameworkCore;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransCompController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransCompController(AppDbContext context)
        {
            _context = context;
        }
        //==================================================================================
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
        //==================================================================================
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
        //==================================================================================

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
        //==================================================================================
        [HttpPost]
        public async Task<IActionResult> Create(TransCompDto dto)
        {
            var transComp = new TransComp
            {
                TransportationID = dto.TransportationID,
                CompanyID = dto.CompanyID
            };

            _context.TransComp.Add(transComp);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //==================================================================================
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
    }
}