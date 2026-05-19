using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server repository for user authentication and audit logging.
/// All queries use parameterised Dapper calls — fully SQL-injection safe.
/// </summary>
public class UserRepository : IUserRepository
{
    static UserRepository()
    {
        // Register custom Dapper type handler: maps NVARCHAR ↔ UserRole enum
        SqlMapper.AddTypeHandler(new UserRoleTypeHandler());
    }

    private SqlConnection GetConnection() => new(ConnectionString.Value);

    /// <summary>
    /// Dapper type handler to convert between NVARCHAR Role column and UserRole enum.
    /// </summary>
    private class UserRoleTypeHandler : SqlMapper.TypeHandler<UserRole>
    {
        public override UserRole Parse(object value) =>
            Enum.TryParse<UserRole>(value?.ToString(), true, out var role) ? role : UserRole.Viewer;

        public override void SetValue(System.Data.IDbDataParameter parameter, UserRole value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = System.Data.DbType.String;
        }
    }


    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(
            "SELECT * FROM AppUsers WHERE Username = @Username",
            new { Username = username.Trim().ToLower() });
    }

    public async Task<AppUser?> GetByIdAsync(int id)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<AppUser>(
            "SELECT * FROM AppUsers WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<AppUser>(
            "SELECT * FROM AppUsers ORDER BY CreatedAt DESC");
    }

    public async Task<int> CreateUserAsync(AppUser user)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO AppUsers
                (Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt)
            VALUES
                (@Username, @PasswordHash, @FullName, @Email, @Role, 1, GETDATE());
            SELECT SCOPE_IDENTITY();",
            new
            {
                Username     = user.Username.Trim().ToLower(),
                user.PasswordHash,
                user.FullName,
                user.Email,
                Role         = user.Role.ToString()
            });
    }

    public async Task UpdateUserAsync(AppUser user)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(@"
            UPDATE AppUsers SET
                FullName = @FullName, Email = @Email,
                Role     = @Role,     IsActive = @IsActive
            WHERE Id = @Id",
            new { user.FullName, user.Email, Role = user.Role.ToString(), user.IsActive, user.Id });
    }

    public async Task RecordFailedLoginAsync(string username)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(@"
            UPDATE AppUsers
            SET FailedLogins = FailedLogins + 1
            WHERE Username = @Username",
            new { Username = username.Trim().ToLower() });
    }

    public async Task ResetFailedLoginsAsync(int userId)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(
            "UPDATE AppUsers SET FailedLogins = 0, IsLocked = 0, LockedUntil = NULL WHERE Id = @Id",
            new { Id = userId });
    }

    public async Task LockUserAsync(int userId, DateTime lockedUntil)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(@"
            UPDATE AppUsers
            SET IsLocked = 1, LockedUntil = @LockedUntil
            WHERE Id = @Id",
            new { Id = userId, LockedUntil = lockedUntil });
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(
            "UPDATE AppUsers SET LastLogin = GETDATE() WHERE Id = @Id",
            new { Id = userId });
    }

    public async Task LogActivityAsync(ActivityLog log)
    {
        using var conn = GetConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO ActivityLogs (UserId, Username, Action, Details, IpAddress, Success, Timestamp)
            VALUES (@UserId, @Username, @Action, @Details, @IpAddress, @Success, GETDATE())",
            new
            {
                log.UserId, log.Username, log.Action,
                log.Details, log.IpAddress, log.Success
            });
    }

    public async Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(int take = 100)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<ActivityLog>(@"
            SELECT TOP (@Take) * FROM ActivityLogs
            ORDER BY Timestamp DESC",
            new { Take = take });
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        using var conn = GetConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AppUsers WHERE Username = @Username",
            new { Username = username.Trim().ToLower() });
        return count > 0;
    }
}
