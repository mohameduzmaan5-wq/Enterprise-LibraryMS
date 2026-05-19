using System.Text;
using LibraryMS.Core.DTOs;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Business-layer service for all report and analytics operations.
/// WinForms UI helpers (dialogs, printing) live in the UI layer.
/// </summary>
public class ReportService
{
    private readonly IReportRepository _repo;

    public ReportService()
    {
        _repo = new ReportRepository();
    }

    // ── Proxy methods ────────────────────────────────────────────
    public Task<IEnumerable<BorrowHistoryRow>>  GetBorrowHistoryAsync(DateTime from, DateTime to)  => _repo.GetBorrowHistoryAsync(from, to);
    public Task<IEnumerable<OverdueReportRow>>  GetOverdueReportAsync()                             => _repo.GetOverdueReportAsync();
    public Task<IEnumerable<TopBookRow>>        GetTopBorrowedBooksAsync(int topN = 10)             => _repo.GetTopBorrowedBooksAsync(topN);
    public Task<IEnumerable<MemberActivityRow>> GetMemberActivityAsync(DateTime from, DateTime to)  => _repo.GetMemberActivityAsync(from, to);
    public Task<IEnumerable<FineReportRow>>     GetFineReportAsync(DateTime from, DateTime to)      => _repo.GetFineReportAsync(from, to);
    public Task<IEnumerable<InventoryRow>>      GetInventoryReportAsync()                           => _repo.GetInventoryReportAsync();
    public Task<IEnumerable<MonthlyTrendRow>>   GetMonthlyTrendsAsync(int months = 12)              => _repo.GetMonthlyTrendsAsync(months);

    // ── CSV builder (pure string — no WinForms types) ────────────
    public static string ToCsv<T>(IEnumerable<T> data, string title)
    {
        var rows  = data.ToList();
        if (rows.Count == 0) return string.Empty;

        var props = typeof(T).GetProperties();
        var sb    = new StringBuilder();

        sb.AppendLine($"# {title}  —  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(string.Join(",", props.Select(p => $"\"{p.Name}\"")));

        foreach (var row in rows)
        {
            var vals = props.Select(p =>
            {
                var v = p.GetValue(row)?.ToString()?.Replace("\"", "\"\"") ?? "";
                return $"\"{v}\"";
            });
            sb.AppendLine(string.Join(",", vals));
        }
        return sb.ToString();
    }

    // ── Text report builder (pure string — no WinForms types) ────
    public static string ToTextReport<T>(
        IEnumerable<T> data, string title,
        string[] columns, Func<T, string[]> rowSelector)
    {
        var rows   = data.ToList();
        var sb     = new StringBuilder();
        const int  width  = 110;
        var border = new string('═', width);

        sb.AppendLine(border);
        sb.AppendLine("  LibraryMS — Enterprise Library Management System");
        sb.AppendLine($"  Report  : {title}");
        sb.AppendLine($"  Created : {DateTime.Now:dddd, MMMM dd yyyy  HH:mm:ss}");
        sb.AppendLine($"  Records : {rows.Count}");
        sb.AppendLine(border);
        sb.AppendLine();

        var colW = columns.Select((c, i) =>
            Math.Max(c.Length,
                rows.Count > 0 ? rows.Max(r => (rowSelector(r)[i] ?? "").Length) : c.Length) + 2
        ).ToArray();

        for (int i = 0; i < columns.Length; i++) sb.Append(columns[i].PadRight(colW[i]));
        sb.AppendLine();
        sb.AppendLine(new string('-', colW.Sum()));

        foreach (var row in rows)
        {
            var cells = rowSelector(row);
            for (int i = 0; i < cells.Length; i++) sb.Append((cells[i] ?? "").PadRight(colW[i]));
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine(border);
        sb.AppendLine("  End of report — LibraryMS v1.0.0 Enterprise Edition");
        sb.AppendLine(border);
        return sb.ToString();
    }
}
