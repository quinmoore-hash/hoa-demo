using System;
using System.Linq;
using System.Threading.Tasks;
using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using HoaDemo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Controllers
{
    [ApiController]
    [Route("api/assessments")]
    public class AssessmentsController : ControllerBase
    {
        private readonly HoaContext _db;

        public AssessmentsController(HoaContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll(int? homeownerId = null)
        {
            var query = _db.Assessments.AsQueryable();
            if (homeownerId.HasValue) query = query.Where(a => a.HomeownerId == homeownerId.Value);

            return Ok(await query.OrderBy(a => a.DueDate).ToListAsync());
        }

        /// <summary>Returns the assessment with its balance and late fee as of today.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
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

        [HttpPost("")]
        public async Task<IActionResult> Create(Assessment assessment)
        {
            if (assessment.Amount <= 0m) return BadRequest(new { message = "Amount must be positive." });
            if (await _db.Homeowners.FindAsync(assessment.HomeownerId) == null) return BadRequest(new { message = "Unknown homeownerId." });

            _db.Assessments.Add(assessment);
            await _db.SaveChangesAsync();
            return Created($"api/assessments/{assessment.Id}", assessment);
        }
    }
}
