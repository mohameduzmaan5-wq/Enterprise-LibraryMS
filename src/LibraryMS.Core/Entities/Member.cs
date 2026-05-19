namespace LibraryMS.Core.Entities;

/// <summary>
/// Represents a library member/patron.
/// </summary>
public class Member : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string MembershipType { get; set; } = "Standard"; // Standard, Premium, Student
    public DateTime JoinDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public int ActiveLoans { get; set; } // Computed field

    /// <summary>
    /// Full name of the member.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Status display text.
    /// </summary>
    public string StatusText => IsActive ? "Active" : "Inactive";
}
