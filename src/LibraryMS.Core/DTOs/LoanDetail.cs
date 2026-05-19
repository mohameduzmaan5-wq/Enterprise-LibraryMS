using LibraryMS.Core.Entities;

namespace LibraryMS.Core.DTOs;

/// <summary>
/// Detailed loan view with joined book and member info.
/// </summary>
public class LoanDetail
{
    public int LoanId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal FineAmount { get; set; }
}
