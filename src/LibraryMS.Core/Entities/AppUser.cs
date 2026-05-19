namespace LibraryMS.Core.Entities;

/// <summary>
/// Roles available in the system.
/// </summary>
public enum UserRole
{
    Admin,
    Librarian,
    Viewer
}

/// <summary>
/// Represents an authenticated system user with role-based access.
/// </summary>
public class AppUser
{
    public int      Id            { get; set; }
    public string   Username      { get; set; } = string.Empty;
    public string   PasswordHash  { get; set; } = string.Empty;  // BCrypt hash
    public string   FullName      { get; set; } = string.Empty;
    public string   Email         { get; set; } = string.Empty;
    public UserRole Role          { get; set; } = UserRole.Librarian;
    public bool     IsActive      { get; set; } = true;
    public int      FailedLogins  { get; set; } = 0;
    public bool     IsLocked      { get; set; } = false;
    public DateTime? LockedUntil  { get; set; }
    public DateTime? LastLogin    { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.Now;

    // Computed
    public bool IsCurrentlyLocked =>
        IsLocked && (LockedUntil == null || LockedUntil > DateTime.Now);

    public string RoleDisplay => Role switch
    {
        UserRole.Admin     => "🛡️ Administrator",
        UserRole.Librarian => "📚 Librarian",
        UserRole.Viewer    => "👁️ Viewer",
        _                  => "Unknown"
    };
}

/// <summary>
/// In-memory session holder — singleton for the current login session.
/// </summary>
public static class AppSession
{
    public static AppUser?  CurrentUser  { get; private set; }
    public static DateTime? LoginTime    { get; private set; }
    public static bool      IsLoggedIn   => CurrentUser != null;

    public static void Begin(AppUser user)
    {
        CurrentUser = user;
        LoginTime   = DateTime.Now;
    }

    public static void End()
    {
        CurrentUser = null;
        LoginTime   = null;
    }

    // ── Permission helpers ────────────────────────────────────
    public static bool CanWrite  => CurrentUser?.Role is UserRole.Admin or UserRole.Librarian;
    public static bool CanDelete => CurrentUser?.Role is UserRole.Admin;
    public static bool IsAdmin   => CurrentUser?.Role == UserRole.Admin;
}

/// <summary>
/// Audit log entry for user activity tracking.
/// </summary>
public class ActivityLog
{
    public int      Id        { get; set; }
    public int?     UserId    { get; set; }
    public string   Username  { get; set; } = string.Empty;
    public string   Action    { get; set; } = string.Empty;
    public string   Details   { get; set; } = string.Empty;
    public string   IpAddress { get; set; } = "localhost";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool     Success   { get; set; } = true;
}
