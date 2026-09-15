using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/company
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companies = await _context.Companies
                .Include(c => c.Phones)
                .Include(c => c.Emails)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            return Ok(companies);
        }

        // GET: api/company/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var company = await _context.Companies
                .Include(c => c.Phones)
                .Include(c => c.Emails)
                .FirstOrDefaultAsync(c => c.CompanyId == id && c.IsActive);

            if (company == null)
                return NotFound(new { success = false, message = $"Company {id} not found." });

            return Ok(company);
        }

        // POST: api/company
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CompanyUpsertRequest request)
        {
            var error = Validate(request);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            var company = new Company
            {
                CompanyName = request.CompanyName.Trim(),
                GstNumber = request.GstNumber.Trim(),
                Address1 = request.Address1?.Trim(),
                Address2 = request.Address2?.Trim(),
                City = request.City?.Trim(),
                State = request.State?.Trim(),
                Zipcode = request.Zipcode?.Trim(),
                IsActive = true,
                DateCreated = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var phones = BuildPhones(company.CompanyId, request.Phones);
            var emails = BuildEmails(company.CompanyId, request.Emails);

            _context.CompanyPhones.AddRange(phones);
            _context.CompanyEmails.AddRange(emails);
            await _context.SaveChangesAsync();

            company.Phones = phones;
            company.Emails = emails;

            return Ok(company);
        }

        // PUT: api/company/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CompanyUpsertRequest request)
        {
            var error = Validate(request);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            var company = await _context.Companies
                .Include(c => c.Phones)
                .Include(c => c.Emails)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null)
                return NotFound(new { success = false, message = $"Company {id} not found." });

            company.CompanyName = request.CompanyName.Trim();
            company.GstNumber = request.GstNumber.Trim();
            company.Address1 = request.Address1?.Trim();
            company.Address2 = request.Address2?.Trim();
            company.City = request.City?.Trim();
            company.State = request.State?.Trim();
            company.Zipcode = request.Zipcode?.Trim();
            company.DateUpdated = DateTime.UtcNow;

            // Replace phones and emails: remove old, add new
            _context.CompanyPhones.RemoveRange(company.Phones);
            _context.CompanyEmails.RemoveRange(company.Emails);

            var phones = BuildPhones(id, request.Phones);
            var emails = BuildEmails(id, request.Emails);

            _context.CompanyPhones.AddRange(phones);
            _context.CompanyEmails.AddRange(emails);

            await _context.SaveChangesAsync();

            company.Phones = phones;
            company.Emails = emails;

            return Ok(company);
        }

        // DELETE: api/company/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {id} not found." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phones = await _context.CompanyPhones.Where(p => p.CompanyId == id).ToListAsync();
                var emails = await _context.CompanyEmails.Where(e => e.CompanyId == id).ToListAsync();
                _context.CompanyPhones.RemoveRange(phones);
                _context.CompanyEmails.RemoveRange(emails);

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Company deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Delete failed: {ex.Message}" });
            }
        }

        private static List<CompanyPhone> BuildPhones(int companyId, List<PhoneRequest>? requested)
        {
            var phones = (requested ?? new List<PhoneRequest>())
                .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
                .Select((p, i) => new CompanyPhone
                {
                    CompanyId = companyId,
                    PhoneNumber = p.PhoneNumber.Trim(),
                    IsPrimary = p.IsPrimary,
                    DisplayOrder = i
                })
                .ToList();

            if (phones.Count > 0 && !phones.Any(p => p.IsPrimary))
                phones[0].IsPrimary = true;

            return phones;
        }

        private static List<CompanyEmail> BuildEmails(int companyId, List<EmailRequest>? requested)
        {
            var emails = (requested ?? new List<EmailRequest>())
                .Where(e => !string.IsNullOrWhiteSpace(e.EmailAddress))
                .Select((e, i) => new CompanyEmail
                {
                    CompanyId = companyId,
                    EmailAddress = e.EmailAddress.Trim(),
                    IsPrimary = e.IsPrimary,
                    DisplayOrder = i
                })
                .ToList();

            if (emails.Count > 0 && !emails.Any(e => e.IsPrimary))
                emails[0].IsPrimary = true;

            return emails;
        }

        private static string? Validate(CompanyUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return "Company name is required.";

            if (string.IsNullOrWhiteSpace(request.GstNumber))
                return "GST number is required.";

            var emails = (request.Emails ?? new List<EmailRequest>())
                .Where(e => !string.IsNullOrWhiteSpace(e.EmailAddress))
                .ToList();

            if (emails.Count == 0)
                return "At least one email address is required.";

            if (emails.Count(e => e.IsPrimary) > 1)
                return "Only one email address can be marked as primary.";

            return null;
        }
    }

    public class CompanyUpsertRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zipcode { get; set; }
        public List<PhoneRequest>? Phones { get; set; }
        public List<EmailRequest>? Emails { get; set; }
    }

    public class PhoneRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class EmailRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
