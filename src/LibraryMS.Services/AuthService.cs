using BCrypt.Net;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Enterprise authentication service.
/// Handles BCrypt password hashing, login attempt limits,
/// account lockout, session management, and activity logging.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepo;

    // Security constants
    private const int  MaxFailedAttempts = 5;
    private const int  LockoutMinutes    = 15;
    private const int  WorkFactor        = 12; // BCrypt cost

    public AuthService()
    {
        _userRepo = new UserRepository();
    }

    // ── Authentication ───────────────────────────────────────────
    /// <summary>
    /// Authenticates a user. Returns (success, message, user).
    /// Enforces lockout, BCrypt verification, and input sanitisation.
    /// </summary>
    public async Task<(bool Success, string Message, AppUser? User)> LoginAsync(
        string username, string password)
    {
        // Input sanitisation
        username = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await AuditAsync(null, username, "LOGIN_FAILED", "Empty credentials", false);
            return (false, "Username and password are required.", null);
        }

        if (username.Length > 50 || password.Length > 128)
        {
            await AuditAsync(null, username, "LOGIN_FAILED", "Oversized input rejected", false);
            return (false, "Invalid input format.", null);
        }

        var user = await _userRepo.GetByUsernameAsync(username);

        if (user == null)
        {
            await AuditAsync(null, username, "LOGIN_FAILED", "User not found", false);
            // Timing-safe: run a real BCrypt operation to prevent user enumeration
            try { BCrypt.Net.BCrypt.HashPassword(password, 4); } catch { }
            return (false, "Invalid username or password.", null);
        }

        // Check account active
        if (!user.IsActive)
        {
            await AuditAsync(user.Id, username, "LOGIN_FAILED", "Inactive account", false);
            return (false, "Your account has been deactivated. Contact an administrator.", null);
        }

        // Check lockout
        if (user.IsCurrentlyLocked)
        {
            var remaining = user.LockedUntil.HasValue
                ? (int)(user.LockedUntil.Value - DateTime.Now).TotalMinutes + 1
                : LockoutMinutes;
            await AuditAsync(user.Id, username, "LOGIN_BLOCKED", "Account locked", false);
            return (false, $"Account locked due to {MaxFailedAttempts} failed attempts.\nTry again in {remaining} minute(s).", null);
        }

        // Verify BCrypt password (trim hash — NVARCHAR may have trailing spaces)
        bool valid;
        try
        {
            valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash?.Trim() ?? "");
        }
        catch
        {
            // Hash is corrupted or invalid format
            valid = false;
        }

        if (!valid)
        {
            await _userRepo.RecordFailedLoginAsync(username);
            var freshUser = await _userRepo.GetByUsernameAsync(username);
            int remaining = MaxFailedAttempts - (freshUser?.FailedLogins ?? 0);

            if (freshUser?.FailedLogins >= MaxFailedAttempts)
            {
                await _userRepo.LockUserAsync(freshUser.Id, DateTime.Now.AddMinutes(LockoutMinutes));
                await AuditAsync(user.Id, username, "ACCOUNT_LOCKED", $"Locked after {MaxFailedAttempts} failures", false);
                return (false, $"Account locked for {LockoutMinutes} minutes after {MaxFailedAttempts} failed attempts.", null);
            }

            await AuditAsync(user.Id, username, "LOGIN_FAILED",
                $"Wrong password. {remaining} attempt(s) remaining", false);
            return (false, $"Invalid username or password. {Math.Max(0, remaining)} attempt(s) remaining.", null);
        }

        // Success
        await _userRepo.ResetFailedLoginsAsync(user.Id);
        await _userRepo.UpdateLastLoginAsync(user.Id);
        await AuditAsync(user.Id, username, "LOGIN_SUCCESS", $"Role: {user.Role}", true);

        AppSession.Begin(user);
        return (true, $"Welcome back, {user.FullName}!", user);
    }

    /// <summary>Ends the current session and logs the logout event.</summary>
    public async Task LogoutAsync()
    {
        if (AppSession.IsLoggedIn && AppSession.CurrentUser != null)
        {
            var duration = AppSession.LoginTime.HasValue
                ? (DateTime.Now - AppSession.LoginTime.Value).ToString(@"hh\:mm\:ss")
                : "—";
            await AuditAsync(AppSession.CurrentUser.Id, AppSession.CurrentUser.Username,
                "LOGOUT", $"Session duration: {duration}", true);
        }
        AppSession.End();
    }

    // ── User management ──────────────────────────────────────────
    public Task<IEnumerable<AppUser>> GetAllUsersAsync() => _userRepo.GetAllAsync();

    /// <summary>Creates a new user with BCrypt-hashed password.</summary>
    public async Task<(bool Success, string Message)> CreateUserAsync(
        string username, string password, string fullName, string email, UserRole role)
    {
        username = username?.Trim().ToLower() ?? "";
        fullName = SanitiseInput(fullName);
        email    = email?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "Username must be at least 3 characters.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-z0-9_\.]+$"))
            return (false, "Username may only contain letters, numbers, underscores, or dots.");
        if (password.Length < 8)
            return (false, "Password must be at least 8 characters.");
        if (string.IsNullOrWhiteSpace(fullName))
            return (false, "Full name is required.");
        if (await _userRepo.UsernameExistsAsync(username))
            return (false, $"Username '{username}' is already taken.");

        var hash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        var user = new AppUser
        {
            Username = username, PasswordHash = hash,
            FullName = fullName, Email = email, Role = role
        };

        await _userRepo.CreateUserAsync(user);
        await AuditAsync(AppSession.CurrentUser?.Id, AppSession.CurrentUser?.Username ?? "system",
            "USER_CREATED", $"Created user '{username}' with role {role}", true);
        return (true, $"User '{username}' created successfully.");
    }

    /// <summary>Changes a user's password — re-hashes with BCrypt.</summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return (false, "User not found.");
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return (false, "Current password is incorrect.");
        if (newPassword.Length < 8)
            return (false, "New password must be at least 8 characters.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, WorkFactor);
        await _userRepo.UpdateUserAsync(user);
        await AuditAsync(userId, user.Username, "PASSWORD_CHANGED", "Password updated", true);
        return (true, "Password changed successfully.");
    }

    // ── Audit log ────────────────────────────────────────────────
    public Task<IEnumerable<ActivityLog>> GetActivityLogsAsync(int take = 200)
        => _userRepo.GetActivityLogsAsync(take);

    private async Task AuditAsync(int? userId, string username, string action, string details, bool success)
    {
        try
        {
            await _userRepo.LogActivityAsync(new ActivityLog
            {
                UserId   = userId,
                Username = username,
                Action   = action,
                Details  = details,
                Success  = success
            });
        }
        catch { /* never let logging break auth flow */ }
    }

    // ── Input sanitisation helper ────────────────────────────────
    private static string SanitiseInput(string? input) =>
        System.Net.WebUtility.HtmlEncode(input?.Trim() ?? string.Empty);

    // ── Static helper for seeding default admin ──────────────────
    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
}
