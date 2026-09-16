using Backend.Data;
using Backend.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend.Services
{
    public class LedgerStatementRow
    {
        public DateTime Date { get; set; }
        public string ToBy { get; set; } = string.Empty;      // "TO" | "BY"
        public string Particulars { get; set; } = string.Empty; // Sales / Bank / Cash / Opening Balance / Closing Balance
        public string VchType { get; set; } = string.Empty;   // derived: To -> Sales, By -> Receipt
        public string VchNo { get; set; } = string.Empty;
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public bool Bold { get; set; }
    }

    public class LedgerStatementData
    {
        public BusinessProfile? Profile { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<LedgerStatementRow> Rows { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }

    // Builds the Excel + PDF ledger statement, matching ArrowInstruments' printed
    // letterhead format (company header, To/By ledger table, closing balance,
    // bank details footer). Shared by the "Download Ledger" export and the
    // auto-attached PDF on email reminders.
    public class LedgerDocumentService
    {
        private readonly AppDbContext _context;

        public LedgerDocumentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LedgerStatementData> BuildStatementDataAsync(int companyId, DateTime start, DateTime end)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId)
                ?? throw new InvalidOperationException($"Company {companyId} not found.");

            var profile = await _context.BusinessProfiles.FirstOrDefaultAsync();

            var entries = await _context.LedgerEntries
                .Where(e => e.CompanyId == companyId && e.EntryDate >= start && e.EntryDate <= end)
                .OrderBy(e => e.EntryDate).ThenBy(e => e.EntryId)
                .ToListAsync();

            var rows = new List<LedgerStatementRow>();

            var openingBalance = company.OpeningBalance;
            var debitSum = 0m;
            var creditSum = 0m;

            if (openingBalance >= 0)
            {
                rows.Add(new LedgerStatementRow { Date = start, ToBy = "TO", Particulars = "OPENING BALANCE", Debit = openingBalance });
                debitSum += openingBalance;
            }
            else
            {
                rows.Add(new LedgerStatementRow { Date = start, ToBy = "BY", Particulars = "OPENING BALANCE", Credit = -openingBalance });
                creditSum += -openingBalance;
            }

            foreach (var e in entries)
            {
                var toBy = (e.Description ?? string.Empty).Trim().Equals("By", StringComparison.OrdinalIgnoreCase) ? "BY" : "TO";
                var vchType = toBy == "TO" ? "SALES" : "RECEIPT";

                rows.Add(new LedgerStatementRow
                {
                    Date = e.EntryDate,
                    ToBy = toBy,
                    Particulars = (e.Type ?? string.Empty).ToUpperInvariant(),
                    VchType = vchType,
                    VchNo = e.TransNo ?? string.Empty,
                    Debit = e.Debit > 0 ? e.Debit : null,
                    Credit = e.Credit > 0 ? e.Credit : null
                });

                debitSum += e.Debit;
                creditSum += e.Credit;
            }

            // Unlabeled running-total row (sums the Debit/Credit columns so far).
            rows.Add(new LedgerStatementRow { Debit = debitSum, Credit = creditSum == 0 ? null : creditSum, Bold = true });

            var diff = debitSum - creditSum;
            decimal grandTotal;
            if (diff >= 0)
            {
                rows.Add(new LedgerStatementRow { ToBy = "BY", Particulars = "CLOSING BALANCE", Credit = diff, Bold = true });
                grandTotal = debitSum;
            }
            else
            {
                rows.Add(new LedgerStatementRow { ToBy = "TO", Particulars = "CLOSING BALANCE", Debit = -diff, Bold = true });
                grandTotal = creditSum;
            }

            return new LedgerStatementData
            {
                Profile = profile,
                CustomerName = company.CompanyName,
                StartDate = start,
                EndDate = end,
                Rows = rows,
                GrandTotal = grandTotal
            };
        }

        private static string PeriodLabel(DateTime start, DateTime end)
        {
            return $"{start:dd-MMM-yyyy} to {end:dd-MMM-yyyy}";
        }

        // ───────────────────────────── Excel ─────────────────────────────

        public byte[] BuildExcel(LedgerStatementData data)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Ledger");
            ws.ColumnWidth = 13;
            ws.Column(3).Width = 20; // Particulars
            ws.Column(4).Width = 12; // Vch Type
            ws.Column(5).Width = 12; // Vch No.
            ws.Column(6).Width = 14; // Debit
            ws.Column(7).Width = 14; // Credit

            var r = 1;
            r = WriteLetterhead(ws, data.Profile, r);

            ws.Cell(r, 1).Value = "TO,";
            ws.Cell(r, 7).Value = PeriodLabel(data.StartDate, data.EndDate);
            ws.Cell(r, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            r++;
            ws.Cell(r, 1).Value = data.CustomerName;
            ws.Cell(r, 1).Style.Font.Bold = true;
            r += 2;

            var headerRow = r;
            var headers = new[] { "DATE", "", "PARTICULARS", "VCH TYPE", "VCH NO.", "DEBIT", "CREDIT" };
            for (var c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            r++;

            foreach (var row in data.Rows)
            {
                if (row.Date != default) ws.Cell(r, 1).Value = row.Date.ToString("d/M/yyyy");
                ws.Cell(r, 2).Value = row.ToBy;
                ws.Cell(r, 3).Value = row.Particulars;
                ws.Cell(r, 4).Value = row.VchType;
                ws.Cell(r, 5).Value = row.VchNo;
                if (row.Debit.HasValue) ws.Cell(r, 6).Value = row.Debit.Value;
                if (row.Credit.HasValue) ws.Cell(r, 7).Value = row.Credit.Value;

                ws.Range(r, 6, r, 7).Style.NumberFormat.Format = "#,##0.00";

                if (row.Bold)
                {
                    ws.Range(r, 1, r, 7).Style.Font.Bold = true;
                    ws.Range(r, 6, r, 7).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    if (row.Particulars == "CLOSING BALANCE")
                        ws.Range(r, 6, r, 7).Style.Font.FontColor = XLColor.Red;
                }
                r++;
            }

            // Grand total row (both sides equal).
            ws.Cell(r, 6).Value = data.GrandTotal;
            ws.Cell(r, 7).Value = data.GrandTotal;
            ws.Range(r, 6, r, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(r, 1, r, 7).Style.Font.Bold = true;
            ws.Range(r, 6, r, 7).Style.Border.TopBorder = XLBorderStyleValues.Double;
            r += 2;

            if (data.Profile != null)
            {
                ws.Cell(r, 1).Value = "Our Bank Details :";
                ws.Cell(r, 1).Style.Font.Bold = true;
                r++;
                ws.Cell(r, 1).Value = $"BANK: {data.Profile.BankName}";
                r++;
                ws.Cell(r, 1).Value = $"BRANCH: {data.Profile.BankBranch}";
                r++;
                ws.Cell(r, 1).Value = $"A/C NO: {data.Profile.BankAccountNo}";
                r++;
                ws.Cell(r, 1).Value = $"IFS CODE: {data.Profile.BankIfscCode}";
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static int WriteLetterhead(IXLWorksheet ws, BusinessProfile? profile, int r)
        {
            void Line(string text, bool bold, int size)
            {
                var cell = ws.Cell(r, 1);
                ws.Range(r, 1, r, 7).Merge();
                cell.Value = text;
                cell.Style.Font.Bold = bold;
                cell.Style.Font.FontSize = size;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                r++;
            }

            if (profile == null)
            {
                Line("ARROW INSTRUMENTS", true, 16);
                r++;
                return r;
            }

            Line(profile.CompanyName, true, 16);
            if (!string.IsNullOrWhiteSpace(profile.Certification)) Line(profile.Certification, true, 10);
            if (!string.IsNullOrWhiteSpace(profile.BusinessLine)) Line(profile.BusinessLine, false, 10);
            if (!string.IsNullOrWhiteSpace(profile.Address)) Line(profile.Address, false, 10);
            if (!string.IsNullOrWhiteSpace(profile.MobileNumbers)) Line($"Mobile : {profile.MobileNumbers}", false, 10);
            if (!string.IsNullOrWhiteSpace(profile.Email)) Line($"email: {profile.Email}", false, 10);
            r++;
            return r;
        }

        // ───────────────────────────── PDF ─────────────────────────────

        public byte[] BuildPdf(LedgerStatementData data)
        {
            var profile = data.Profile;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(profile?.CompanyName ?? "ARROW INSTRUMENTS").Bold().FontSize(16);
                        if (!string.IsNullOrWhiteSpace(profile?.Certification))
                            col.Item().AlignCenter().Text(profile.Certification).Bold().FontSize(10);
                        if (!string.IsNullOrWhiteSpace(profile?.BusinessLine))
                            col.Item().AlignCenter().Text(profile.BusinessLine).FontSize(10);
                        if (!string.IsNullOrWhiteSpace(profile?.Address))
                            col.Item().AlignCenter().Text(profile.Address).FontSize(10);
                        if (!string.IsNullOrWhiteSpace(profile?.MobileNumbers))
                            col.Item().AlignCenter().Text($"Mobile : {profile.MobileNumbers}").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(profile?.Email))
                            col.Item().AlignCenter().Text($"email: {profile.Email}").FontSize(10);

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("TO,").Bold();
                            row.RelativeItem().AlignRight().Text(PeriodLabel(data.StartDate, data.EndDate)).Bold();
                        });
                        col.Item().Text(data.CustomerName).Bold();
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);   // Date
                            columns.RelativeColumn(1);   // To/By
                            columns.RelativeColumn(2.5f); // Particulars
                            columns.RelativeColumn(2);   // Vch Type
                            columns.RelativeColumn(2);   // Vch No
                            columns.RelativeColumn(2.5f); // Debit
                            columns.RelativeColumn(2.5f); // Credit
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(string text, bool right = false)
                            {
                                var cell = header.Cell().BorderBottom(1).BorderTop(1).PaddingVertical(4);
                                (right ? cell.AlignRight() : cell.AlignLeft()).Text(text).Bold();
                            }

                            HeaderCell("DATE");
                            HeaderCell("");
                            HeaderCell("PARTICULARS");
                            HeaderCell("VCH TYPE");
                            HeaderCell("VCH NO.");
                            HeaderCell("DEBIT", right: true);
                            HeaderCell("CREDIT", right: true);
                        });

                        foreach (var row in data.Rows)
                        {
                            void Cell(string text, bool right = false, bool bold = false, string? color = null)
                            {
                                var c = table.Cell().PaddingVertical(3);
                                if (row.Bold) c = c.DefaultTextStyle(x => x.Bold());
                                var aligned = right ? c.AlignRight() : c.AlignLeft();
                                var t = aligned.Text(text);
                                if (color != null) t.FontColor(color);
                            }

                            Cell(row.Date != default ? row.Date.ToString("d/M/yyyy") : "");
                            Cell(row.ToBy);
                            Cell(row.Particulars);
                            Cell(row.VchType);
                            Cell(row.VchNo);
                            Cell(row.Debit?.ToString("#,##0.00") ?? "", right: true,
                                color: row.Bold && row.Particulars == "CLOSING BALANCE" ? Colors.Red.Medium : (string?)null);
                            Cell(row.Credit?.ToString("#,##0.00") ?? "", right: true,
                                color: row.Bold && row.Particulars == "CLOSING BALANCE" ? Colors.Red.Medium : (string?)null);
                        }

                        table.Cell().ColumnSpan(5).BorderTop(1).PaddingVertical(4);
                        table.Cell().BorderTop(1).PaddingVertical(4).AlignRight().Text(data.GrandTotal.ToString("#,##0.00")).Bold();
                        table.Cell().BorderTop(1).PaddingVertical(4).AlignRight().Text(data.GrandTotal.ToString("#,##0.00")).Bold();
                    });

                    if (profile != null)
                    {
                        page.Footer().PaddingTop(10).Column(col =>
                        {
                            col.Item().Text("Our Bank Details :").Bold();
                            col.Item().Text($"BANK: {profile.BankName}");
                            col.Item().Text($"BRANCH: {profile.BankBranch}");
                            col.Item().Text($"A/C NO: {profile.BankAccountNo}");
                            col.Item().Text($"IFS CODE: {profile.BankIfscCode}");
                        });
                    }
                });
            }).GeneratePdf();
        }
    }
}
