using LibraryMS.Core.DTOs;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Business logic service for Loan operations.
/// </summary>
public class LoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;

    public LoanService()
    {
        _loanRepository = new LoanRepository();
        _bookRepository = new BookRepository();
    }

    public Task<IEnumerable<Loan>> GetAllLoansAsync()        => _loanRepository.GetAllAsync();
    public Task<IEnumerable<Loan>> GetActiveLoansAsync()     => _loanRepository.GetActiveLoansAsync();
    public Task<IEnumerable<Loan>> GetOverdueLoansAsync()    => _loanRepository.GetOverdueLoansAsync();
    public Task<IEnumerable<LoanDetail>> GetLoanDetailsAsync() => _loanRepository.GetLoanDetailsAsync();
    public Task<int>  GetActiveCountAsync()                  => _loanRepository.GetActiveCountAsync();
    public Task<int>  GetOverdueCountAsync()                 => _loanRepository.GetOverdueCountAsync();
    public Task<IEnumerable<Loan>> GetMemberLoansAsync(int memberId) => _loanRepository.GetMemberLoansAsync(memberId);
    public Task<IEnumerable<Loan>> SearchLoansAsync(string term)     => _loanRepository.SearchAsync(term);
    public Task<IEnumerable<Loan>> GetReturnHistoryAsync()           => _loanRepository.GetReturnHistoryAsync();
    public Task<decimal> GetOutstandingFinesAsync()                  => _loanRepository.GetOutstandingFinesAsync();
    public Task<decimal> GetTotalFinesCollectedAsync()               => _loanRepository.GetTotalFinesAsync();

    /// <summary>
    /// Computes the real-time fine for an active loan using the 7-day grace rule.
    /// Fine = $1.00/day charged only after 7 grace days beyond due date.
    /// </summary>
    public static decimal ComputeFine(DateTime dueDate, DateTime? returnDate = null)
    {
        const int    GraceDays  = 7;
        const decimal FinePerDay = 1.00m;
        var now        = returnDate ?? DateTime.Now;
        var overdueDays = (int)(now - dueDate).TotalDays;
        if (overdueDays <= GraceDays) return 0m;
        return (overdueDays - GraceDays) * FinePerDay;
    }

    /// <summary>
    /// Issues a book to a member with full enterprise validation.
    /// </summary>
    public async Task<(bool Success, string Message)> IssueBookAsync(int bookId, int memberId, int loanDays = 14)
    {
        if (loanDays < 1 || loanDays > 180)
            return (false, "Loan duration must be between 1 and 180 days.");

        // Validate book availability
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            return (false, "Book not found.");
        if (book.AvailableQuantity <= 0)
            return (false, $"'{book.Title}' is currently not available. All copies are checked out.");

        // Prevent duplicate active loan for same book + member
        var isDuplicate = await _loanRepository.CheckActiveLoanExistsAsync(bookId, memberId);
        if (isDuplicate)
            return (false, $"This member already has an active loan for '{book.Title}'.");

        var loan = new Loan
        {
            BookId  = bookId,
            MemberId = memberId,
            BorrowDate = DateTime.Now,
            DueDate    = DateTime.Now.AddDays(loanDays),
            Status = "Active"
        };

        await _loanRepository.AddAsync(loan);
        return (true, $"'{book.Title}' issued successfully!  Due: {loan.DueDate:MMM dd, yyyy}");
    }

    /// <summary>
    /// Returns a book. Fine applies only after 7 grace days beyond due date ($1.00/day).
    /// </summary>
    public async Task<(bool Success, string Message)> ReturnBookAsync(int loanId)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId);
        if (loan == null)
            return (false, "Loan record not found.");
        if (loan.Status == "Returned")
            return (false, "This book has already been returned.");

        var fine = ComputeFine(loan.DueDate);

        var result = await _loanRepository.ReturnBookAsync(loanId, fine);
        if (!result)
            return (false, "Failed to process the return. Please try again.");

        var overdueDays = (int)(DateTime.Now - loan.DueDate).TotalDays;
        string msg;
        if (fine > 0)
            msg = $"Book returned. Fine: ${fine:F2}  ({overdueDays} days overdue, 7-day grace applied).";
        else if (overdueDays > 0)
            msg = $"Book returned within grace period ({overdueDays}d overdue) — no fine.";
        else
            msg = "Book returned on time — no fine. ✓";

        return (true, msg);
    }
}
