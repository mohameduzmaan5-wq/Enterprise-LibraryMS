using LibraryMS.Core.DTOs;
using LibraryMS.Core.Entities;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository interface for Loan entity operations.
/// </summary>
public interface ILoanRepository
{
    Task<IEnumerable<Loan>> GetAllAsync();
    Task<IEnumerable<Loan>> GetActiveLoansAsync();
    Task<IEnumerable<Loan>> GetOverdueLoansAsync();
    Task<IEnumerable<LoanDetail>> GetLoanDetailsAsync();
    Task<Loan?> GetByIdAsync(int id);
    Task<int> AddAsync(Loan loan);
    Task<bool> ReturnBookAsync(int loanId, decimal fineAmount = 0);
    Task<int> GetActiveCountAsync();
    Task<int> GetOverdueCountAsync();
    Task<int> GetLoansThisMonthCountAsync();
    Task<int> GetReturnsThisMonthCountAsync();
    Task<decimal> GetTotalFinesAsync();
    Task<IEnumerable<Loan>> GetMemberLoansAsync(int memberId);
    Task<IEnumerable<Loan>> SearchAsync(string term);
    Task<bool> CheckActiveLoanExistsAsync(int bookId, int memberId);
    Task<IEnumerable<Loan>> GetReturnHistoryAsync();
    Task<decimal> GetOutstandingFinesAsync();
}
