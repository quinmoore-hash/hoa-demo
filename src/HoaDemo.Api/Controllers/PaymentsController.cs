using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;

namespace HoaDemo.Api.Controllers
{
    [RoutePrefix("api/payments")]
    public class PaymentsController : ApiController
    {
        private readonly HoaContext _db = new HoaContext();

        [HttpGet, Route("")]
        public async Task<IHttpActionResult> GetAll(int? assessmentId = null)
        {
            var query = _db.Payments.AsQueryable();
            if (assessmentId.HasValue) query = query.Where(p => p.AssessmentId == assessmentId.Value);

            return Ok(await query.OrderByDescending(p => p.PaidOn).ToListAsync());
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(Payment payment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (payment.Amount <= 0m) return BadRequest("Amount must be positive.");
            if (await _db.Assessments.FindAsync(payment.AssessmentId) == null) return BadRequest("Unknown assessmentId.");

            if (payment.PaidOn == default(DateTime)) payment.PaidOn = DateTime.Today;

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            return Created($"api/payments/{payment.Id}", payment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
