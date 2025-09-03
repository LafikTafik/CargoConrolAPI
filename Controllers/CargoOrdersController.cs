using Microsoft.AspNetCore.Mvc;
using CCAPI.Models;
using CCAPI.DTO.defaultt;
using Microsoft.EntityFrameworkCore;

namespace CCAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CargoOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CargoOrdersController(AppDbContext context)
        {
            _context = context;
        }

        //==================================================================================
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var items = await _context.CargoOrders
                .Where(co => co.OrderID == orderId)
                .Select(co => new CargoOrdersDto
                {
                    OrderID = co.OrderID,
                    CargoID = co.CargoID
                })
                .ToListAsync();

            return Ok(items);
        }

        //==================================================================================
        [HttpPost]
        public async Task<IActionResult> Create(CargoOrdersDto dto)
        {
            var cargoOrder = new CargoOrders
            {
                OrderID = dto.OrderID,
                CargoID = dto.CargoID
            };

            _context.CargoOrders.Add(cargoOrder);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //==================================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var links = await _context.CargoOrders
                .Select(co => new CargoOrdersDto
                {
                    CargoID = co.CargoID,
                    OrderID = co.OrderID
                })
                .ToListAsync();

            return Ok(links);
        }

        //==================================================================================
        [HttpGet("{cargoId}/{orderId}")]
        public async Task<IActionResult> GetByCompositeKey(int cargoId, int orderId)
        {
            var link = await _context.CargoOrders
                .Where(co => co.CargoID == cargoId && co.OrderID == orderId)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            var dto = new CargoOrdersDto
            {
                CargoID = link.CargoID,
                OrderID = link.OrderID
            };

            return Ok(dto);
        }

        //==================================================================================
        [HttpDelete("{cargoId}/{orderId}")]
        public async Task<IActionResult> Delete(int cargoId, int orderId)
        {
            var link = await _context.CargoOrders
                .Where(co => co.CargoID == cargoId && co.OrderID == orderId)
                .FirstOrDefaultAsync();

            if (link == null) return NotFound();

            _context.CargoOrders.Remove(link);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}