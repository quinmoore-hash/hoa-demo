using HoaDemo.Api.Data;
using HoaDemo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoaDemo.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly HoaContext _db;
        private readonly TimeProvider _clock;

        public PaymentsController(HoaContext db, TimeProvider clock)
        {
            _db = db;
            _clock = clock;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll(int? assessmentId = null)
        {
            var query = _db.Payments.AsQueryable();
            if (assessmentId.HasValue) query = query.Where(p => p.AssessmentId == assessmentId.Value);

            return Ok(await query.OrderByDescending(p => p.PaidOn).ToListAsync());
        }

        [HttpPost("")]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (payment.Amount <= 0m) return BadRequest(new { message = "Amount must be positive." });
            if (!await _db.Assessments.AnyAsync(a => a.Id == payment.AssessmentId)) return BadRequest(new { message = "Unknown assessmentId." });

            if (payment.PaidOn == default) payment.PaidOn = _clock.GetLocalNow().Date;

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            return Created($"api/payments/{payment.Id}", payment);
        }
    }
}
