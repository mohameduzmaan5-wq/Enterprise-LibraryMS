namespace LibraryMS.Core.Entities;

/// <summary>
/// Represents a book loan/checkout transaction.
/// </summary>
public class Loan : BaseEntity
{
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public DateTime BorrowDate { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Returned, Overdue
    public decimal FineAmount { get; set; }

    // Joined fields for display
    public string? BookTitle { get; set; }
    public string? MemberName { get; set; }

    /// <summary>
    /// Whether the loan is overdue.
    /// </summary>
    public bool IsOverdue => Status == "Active" && DateTime.Now > DueDate;

    /// <summary>
    /// Days remaining or overdue.
    /// </summary>
    public int DaysRemaining => (DueDate - DateTime.Now).Days;
}
