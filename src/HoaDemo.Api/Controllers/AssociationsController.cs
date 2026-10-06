using System.Linq;
using System.Threading.Tasks;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Controllers
{
    [ApiController]
    [Route("api/associations")]
    public class AssociationsController : ControllerBase
    {
        private readonly HoaContext _db;

        public AssociationsController(HoaContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _db.Associations.OrderBy(a => a.Name).ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var association = await _db.Associations.FindAsync(id);
            return association == null ? NotFound() : Ok(association);
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(Association association)
        {
            _db.Associations.Add(association);
            await _db.SaveChangesAsync();
            return Created($"api/associations/{association.Id}", association);
        }
    }
}
