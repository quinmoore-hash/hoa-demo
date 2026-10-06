using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Controllers
{
    [RoutePrefix("api/associations")]
    public class AssociationsController : ApiController
    {
        private readonly HoaContext _db = new HoaContext();

        [HttpGet, Route("")]
        public async Task<IHttpActionResult> GetAll()
        {
            return Ok(await _db.Associations.OrderBy(a => a.Name).ToListAsync());
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            var association = await _db.Associations.FindAsync(id);
            return association == null ? (IHttpActionResult)NotFound() : Ok(association);
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(Association association)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _db.Associations.Add(association);
            await _db.SaveChangesAsync();
            return Created($"api/associations/{association.Id}", association);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
