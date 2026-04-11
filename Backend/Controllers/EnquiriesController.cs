using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/enquiries")]
    [Authorize(Roles = "admin")]
    public class EnquiriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnquiriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/enquiries
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var enquiries = await _context.ContactSubmissions
                .OrderByDescending(e => e.SubmittedAt)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.CompanyName,
                    e.Email,
                    e.Phone,
                    e.Message,
                    e.IsRead,
                    status = e.IsRead ? "read" : "new",
                    e.SubmittedAt
                })
                .ToListAsync();

            return Ok(enquiries);
        }

        // PUT: api/enquiries/:id
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnquiryRequest request)
        {
            var enquiry = await _context.ContactSubmissions.FindAsync(id);
            if (enquiry == null)
                return NotFound(new { success = false, message = "Enquiry not found." });

            enquiry.IsRead = request.IsRead;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // DELETE: api/enquiries/:id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var enquiry = await _context.ContactSubmissions.FindAsync(id);
            if (enquiry == null)
                return NotFound(new { success = false, message = "Enquiry not found." });

            _context.ContactSubmissions.Remove(enquiry);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class UpdateEnquiryRequest
    {
        public bool IsRead { get; set; }
    }
}
