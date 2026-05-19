using LibraryMS.Core.Entities;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository contract for user authentication and activity logging.
/// </summary>
public interface IUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<AppUser?> GetByIdAsync(int id);
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<int> CreateUserAsync(AppUser user);
    Task UpdateUserAsync(AppUser user);
    Task RecordFailedLoginAsync(string username);
    Task ResetFailedLoginsAsync(int userId);
    Task LockUserAsync(int userId, DateTime lockedUntil);
    Task UpdateLastLoginAsync(int userId);
    Task LogActivityAsync(ActivityLog log);
    Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(int take = 100);
    Task<bool> UsernameExistsAsync(string username);
}
