using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "admin")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly LedgerDocumentService _documentService;

        public ReportsController(AppDbContext context, LedgerDocumentService documentService)
        {
            _context = context;
            _documentService = documentService;
        }

        // GET: api/reports/customers?startDate=2026-01-01&endDate=2026-09-13
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomerSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var (start, end) = ResolveRange(startDate, endDate);

            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            var companyIds = companies.Select(c => c.CompanyId).ToList();

            var periodTotals = await _context.LedgerEntries
                .Where(e => companyIds.Contains(e.CompanyId) && e.EntryDate >= start && e.EntryDate <= end)
                .GroupBy(e => e.CompanyId)
                .Select(g => new { CompanyId = g.Key, Debit = g.Sum(e => e.Debit), Credit = g.Sum(e => e.Credit) })
                .ToDictionaryAsync(x => x.CompanyId, x => x);

            var rows = companies.Select(c =>
            {
                var open = c.OpeningBalance;
                var hasPeriod = periodTotals.TryGetValue(c.CompanyId, out var p);
                var debit = hasPeriod ? p!.Debit : 0m;
                var credit = hasPeriod ? p!.Credit : 0m;

                return new
                {
                    companyId = c.CompanyId,
                    companyName = c.CompanyName,
                    open,
                    debit,
                    credit,
                    balance = open + debit - credit
                };
            }).ToList();

            return Ok(new { startDate = start, endDate = end, rows });
        }

        // GET: api/reports/customers/5?startDate=2026-01-01&endDate=2026-09-13
        [HttpGet("customers/{companyId}")]
        public async Task<IActionResult> GetCustomerLedger(int companyId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsActive);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            var (start, end) = ResolveRange(startDate, endDate);

            var openingBalance = company.OpeningBalance;

            var entries = await _context.LedgerEntries
                .Where(e => e.CompanyId == companyId && e.EntryDate >= start && e.EntryDate <= end)
                .OrderBy(e => e.EntryDate).ThenBy(e => e.EntryId)
                .ToListAsync();

            var running = openingBalance;
            var rows = new List<object>();
            foreach (var e in entries)
            {
                var open = running;
                running += e.Debit - e.Credit;
                rows.Add(new
                {
                    entryId = e.EntryId,
                    date = e.EntryDate,
                    description = e.Description,
                    type = e.Type,
                    transNo = e.TransNo,
                    open,
                    debit = e.Debit,
                    credit = e.Credit,
                    balance = running
                });
            }

            return Ok(new
            {
                companyId = company.CompanyId,
                companyName = company.CompanyName,
                startDate = start,
                endDate = end,
                openingBalance,
                closingBalance = running,
                rows
            });
        }

        // GET: api/reports/customers/5/ledger/excel?startDate=2026-01-01&endDate=2026-09-13
        [HttpGet("customers/{companyId}/ledger/excel")]
        public async Task<IActionResult> ExportLedgerExcel(int companyId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var (start, end) = ResolveRange(startDate, endDate);

            LedgerStatementData data;
            try
            {
                data = await _documentService.BuildStatementDataAsync(companyId, start, end);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }

            var bytes = _documentService.BuildExcel(data);
            var fileName = $"{data.CustomerName} Ledger Statement {start:yyyyMMdd}-{end:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: api/reports/customers/5/ledger/pdf?startDate=2026-01-01&endDate=2026-09-13
        [HttpGet("customers/{companyId}/ledger/pdf")]
        public async Task<IActionResult> ExportLedgerPdf(int companyId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var (start, end) = ResolveRange(startDate, endDate);

            LedgerStatementData data;
            try
            {
                data = await _documentService.BuildStatementDataAsync(companyId, start, end);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }

            var bytes = _documentService.BuildPdf(data);
            var fileName = $"{data.CustomerName} Ledger Statement {start:yyyyMMdd}-{end:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // POST: api/reports/customers/5/entries
        [HttpPost("customers/{companyId}/entries")]
        public async Task<IActionResult> CreateEntry(int companyId, [FromBody] LedgerEntryRequest request)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            var error = ValidateEntry(request);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            var entry = new LedgerEntry
            {
                CompanyId = companyId,
                EntryDate = request.EntryDate.Date,
                Description = request.Description?.Trim(),
                Type = request.Type?.Trim(),
                TransNo = request.TransNo?.Trim(),
                Debit = request.Debit,
                Credit = request.Credit,
                DateCreated = DateTime.UtcNow
            };

            _context.LedgerEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(entry);
        }

        // PUT: api/reports/customers/5/entries/12
        [HttpPut("customers/{companyId}/entries/{entryId}")]
        public async Task<IActionResult> UpdateEntry(int companyId, int entryId, [FromBody] LedgerEntryRequest request)
        {
            var entry = await _context.LedgerEntries
                .FirstOrDefaultAsync(e => e.EntryId == entryId && e.CompanyId == companyId);

            if (entry == null)
                return NotFound(new { success = false, message = "Entry not found." });

            var error = ValidateEntry(request);
            if (error != null)
                return BadRequest(new { success = false, message = error });

            entry.EntryDate = request.EntryDate.Date;
            entry.Description = request.Description?.Trim();
            entry.Type = request.Type?.Trim();
            entry.TransNo = request.TransNo?.Trim();
            entry.Debit = request.Debit;
            entry.Credit = request.Credit;

            await _context.SaveChangesAsync();

            return Ok(entry);
        }

        // POST: api/reports/customers/5/opening-balance  (adds to the existing value)
        [HttpPost("customers/{companyId}/opening-balance")]
        public async Task<IActionResult> AddOpeningBalance(int companyId, [FromBody] OpeningBalanceRequest request)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            if (request.Amount == 0)
                return BadRequest(new { success = false, message = "Enter a non-zero amount." });

            company.OpeningBalance += request.Amount;
            company.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { companyId = company.CompanyId, openingBalance = company.OpeningBalance });
        }

        // PUT: api/reports/customers/5/opening-balance  (sets the value directly)
        [HttpPut("customers/{companyId}/opening-balance")]
        public async Task<IActionResult> SetOpeningBalance(int companyId, [FromBody] OpeningBalanceRequest request)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound(new { success = false, message = $"Company {companyId} not found." });

            company.OpeningBalance = request.Amount;
            company.DateUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { companyId = company.CompanyId, openingBalance = company.OpeningBalance });
        }

        // DELETE: api/reports/customers/5/entries/12
        [HttpDelete("customers/{companyId}/entries/{entryId}")]
        public async Task<IActionResult> DeleteEntry(int companyId, int entryId)
        {
            var entry = await _context.LedgerEntries
                .FirstOrDefaultAsync(e => e.EntryId == entryId && e.CompanyId == companyId);

            if (entry == null)
                return NotFound(new { success = false, message = "Entry not found." });

            _context.LedgerEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Entry deleted." });
        }

        // Default range: 1 Jan of the current year through today.
        private static (DateTime start, DateTime end) ResolveRange(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.UtcNow.Date;
            var start = startDate?.Date ?? new DateTime(today.Year, 1, 1);
            var end = endDate?.Date ?? today;
            return (start, end);
        }

        private static string? ValidateEntry(LedgerEntryRequest request)
        {
            if (request.EntryDate == default)
                return "Entry date is required.";

            if (request.Debit < 0 || request.Credit < 0)
                return "Debit and credit amounts cannot be negative.";

            if (request.Debit == 0 && request.Credit == 0)
                return "Enter either a debit or a credit amount.";

            if (request.Debit > 0 && request.Credit > 0)
                return "An entry cannot have both a debit and a credit amount.";

            return null;
        }
    }

    public class LedgerEntryRequest
    {
        public DateTime EntryDate { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? TransNo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class OpeningBalanceRequest
    {
        public decimal Amount { get; set; }
    }
}
