using LibraryMS.Core.DTOs;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository interface for all analytics and report queries.
/// </summary>
public interface IReportRepository
{
    Task<IEnumerable<BorrowHistoryRow>>   GetBorrowHistoryAsync(DateTime from, DateTime to);
    Task<IEnumerable<OverdueReportRow>>   GetOverdueReportAsync();
    Task<IEnumerable<TopBookRow>>         GetTopBorrowedBooksAsync(int topN = 10);
    Task<IEnumerable<MemberActivityRow>>  GetMemberActivityAsync(DateTime from, DateTime to);
    Task<IEnumerable<FineReportRow>>      GetFineReportAsync(DateTime from, DateTime to);
    Task<IEnumerable<InventoryRow>>       GetInventoryReportAsync();
    Task<IEnumerable<MonthlyTrendRow>>    GetMonthlyTrendsAsync(int months = 12);
}
