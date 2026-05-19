namespace LibraryMS.Core.DTOs;

/// <summary>
/// Top-borrowed book analytics row.
/// </summary>
public class TopBookRow
{
    public string Title      { get; set; } = string.Empty;
    public string Author     { get; set; } = string.Empty;
    public string Category   { get; set; } = string.Empty;
    public int    BorrowCount { get; set; }
    public int    Quantity    { get; set; }
    public int    Available   { get; set; }
}

/// <summary>
/// Overdue loan report row.
/// </summary>
public class OverdueReportRow
{
    public int     LoanId      { get; set; }
    public string  BookTitle   { get; set; } = string.Empty;
    public string  MemberName  { get; set; } = string.Empty;
    public string  MemberEmail { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate    { get; set; }
    public int      DaysOverdue { get; set; }
    public decimal  AccruedFine { get; set; }
}

/// <summary>
/// Member activity report row.
/// </summary>
public class MemberActivityRow
{
    public int     MemberId       { get; set; }
    public string  MemberName     { get; set; } = string.Empty;
    public string  MembershipType { get; set; } = string.Empty;
    public int     TotalLoans     { get; set; }
    public int     ActiveLoans    { get; set; }
    public int     ReturnedLoans  { get; set; }
    public decimal TotalFines     { get; set; }
    public DateTime LastActivity  { get; set; }
}

/// <summary>
/// Fine / collection report row.
/// </summary>
public class FineReportRow
{
    public int      LoanId     { get; set; }
    public string   BookTitle  { get; set; } = string.Empty;
    public string   MemberName { get; set; } = string.Empty;
    public DateTime DueDate    { get; set; }
    public DateTime ReturnDate { get; set; }
    public int      OverdueDays { get; set; }
    public decimal  FineAmount  { get; set; }
    public string   Status      { get; set; } = string.Empty;
}

/// <summary>
/// Borrow history report row.
/// </summary>
public class BorrowHistoryRow
{
    public int      LoanId      { get; set; }
    public string   BookTitle   { get; set; } = string.Empty;
    public string   Author      { get; set; } = string.Empty;
    public string   MemberName  { get; set; } = string.Empty;
    public DateTime BorrowDate  { get; set; }
    public DateTime DueDate     { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string   Status      { get; set; } = string.Empty;
    public decimal  FineAmount  { get; set; }
}

/// <summary>
/// Inventory snapshot row.
/// </summary>
public class InventoryRow
{
    public int    BookId    { get; set; }
    public string Title     { get; set; } = string.Empty;
    public string Author    { get; set; } = string.Empty;
    public string Category  { get; set; } = string.Empty;
    public string ISBN      { get; set; } = string.Empty;
    public int    Quantity  { get; set; }
    public int    Available { get; set; }
    public int    CheckedOut { get; set; }
    public string Status    { get; set; } = string.Empty;
}

/// <summary>
/// Monthly trend data point (for chart rendering).
/// </summary>
public class MonthlyTrendRow
{
    public int    Year  { get; set; }
    public int    Month { get; set; }
    public int    Loans { get; set; }
    public int    Returns { get; set; }
    public decimal Fines { get; set; }
    public string Label => $"{new DateTime(Year, Month, 1):MMM yy}";
}
