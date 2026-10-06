using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Controllers
{
    [RoutePrefix("api/homeowners")]
    public class HomeownersController : ApiController
    {
        private readonly HoaContext _db = new HoaContext();

        [HttpGet, Route("")]
        public async Task<IHttpActionResult> GetAll(int? associationId = null)
        {
            var query = _db.Homeowners.AsQueryable();
            if (associationId.HasValue) query = query.Where(h => h.AssociationId == associationId.Value);

            return Ok(await query.OrderBy(h => h.LastName).ThenBy(h => h.FirstName).ToListAsync());
        }

        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            var homeowner = await _db.Homeowners.FindAsync(id);
            return homeowner == null ? (IHttpActionResult)NotFound() : Ok(homeowner);
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(Homeowner homeowner)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (await _db.Associations.FindAsync(homeowner.AssociationId) == null) return BadRequest("Unknown associationId.");

            _db.Homeowners.Add(homeowner);
            await _db.SaveChangesAsync();
            return Created($"api/homeowners/{homeowner.Id}", homeowner);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
