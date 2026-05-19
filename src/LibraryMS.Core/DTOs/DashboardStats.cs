namespace LibraryMS.Core.DTOs;

/// <summary>
/// Dashboard statistics data transfer object.
/// </summary>
public class DashboardStats
{
    public int TotalBooks { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int BooksAddedThisMonth { get; set; }
    public int NewMembersThisMonth { get; set; }
    public int LoansThisMonth { get; set; }
    public int ReturnsThisMonth { get; set; }
    public decimal TotalFinesCollected { get; set; }
}
