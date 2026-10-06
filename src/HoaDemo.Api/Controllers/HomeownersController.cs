using System.Linq;
using System.Threading.Tasks;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Controllers
{
    [ApiController]
    [Route("api/homeowners")]
    public class HomeownersController : ControllerBase
    {
        private readonly HoaContext _db;

        public HomeownersController(HoaContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll(int? associationId = null)
        {
            var query = _db.Homeowners.AsQueryable();
            if (associationId.HasValue) query = query.Where(h => h.AssociationId == associationId.Value);

            return Ok(await query.OrderBy(h => h.LastName).ThenBy(h => h.FirstName).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var homeowner = await _db.Homeowners.FindAsync(id);
            return homeowner == null ? NotFound() : Ok(homeowner);
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(Homeowner homeowner)
        {
            if (await _db.Associations.FindAsync(homeowner.AssociationId) == null) return BadRequest(new { message = "Unknown associationId." });

            _db.Homeowners.Add(homeowner);
            await _db.SaveChangesAsync();
            return Created($"api/homeowners/{homeowner.Id}", homeowner);
        }
    }
}
