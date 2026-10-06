using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using HoaDemo.Api.Services;

namespace HoaDemo.Api.Controllers
{
    [RoutePrefix("api/assessments")]
    public class AssessmentsController : ApiController
    {
        private readonly HoaContext _db = new HoaContext();

        [HttpGet, Route("")]
        public async Task<IHttpActionResult> GetAll(int? homeownerId = null)
        {
            var query = _db.Assessments.AsQueryable();
            if (homeownerId.HasValue) query = query.Where(a => a.HomeownerId == homeownerId.Value);

            return Ok(await query.OrderBy(a => a.DueDate).ToListAsync());
        }

        /// <summary>Returns the assessment with its balance and late fee as of today.</summary>
        [HttpGet, Route("{id:int}")]
        public async Task<IHttpActionResult> Get(int id)
        {
            var assessment = await _db.Assessments
                .Include(a => a.Payments)
                .Include(a => a.Homeowner.Association)
                .SingleOrDefaultAsync(a => a.Id == id);

            if (assessment == null) return NotFound();

            var today = DateTime.Today;
            var balance = LateFeeCalculator.OutstandingBalance(assessment, today);
            var lateFee = LateFeeCalculator.CalculateLateFee(assessment, assessment.Homeowner.Association, today);

            return Ok(new
            {
                assessment.Id,
                assessment.HomeownerId,
                assessment.Description,
                assessment.Amount,
                assessment.DueDate,
                Balance = balance,
                LateFee = lateFee,
                TotalDue = balance + lateFee,
                Payments = assessment.Payments.Select(p => new { p.Id, p.Amount, p.PaidOn, p.Method, p.ReferenceNumber })
            });
        }

        [HttpPost, Route("")]
        public async Task<IHttpActionResult> Create(Assessment assessment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (assessment.Amount <= 0m) return BadRequest("Amount must be positive.");
            if (await _db.Homeowners.FindAsync(assessment.HomeownerId) == null) return BadRequest("Unknown homeownerId.");

            _db.Assessments.Add(assessment);
            await _db.SaveChangesAsync();
            return Created($"api/assessments/{assessment.Id}", assessment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
