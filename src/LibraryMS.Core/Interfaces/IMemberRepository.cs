using LibraryMS.Core.Entities;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository interface for Member entity operations.
/// </summary>
public interface IMemberRepository
{
    Task<IEnumerable<Member>> GetAllAsync();
    Task<Member?> GetByIdAsync(int id);
    Task<IEnumerable<Member>> SearchAsync(string searchTerm);
    Task<int> AddAsync(Member member);
    Task<bool> UpdateAsync(Member member);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalCountAsync();
    Task<int> GetNewThisMonthCountAsync();
    Task<IEnumerable<Member>> GetActiveMembersAsync();
}
