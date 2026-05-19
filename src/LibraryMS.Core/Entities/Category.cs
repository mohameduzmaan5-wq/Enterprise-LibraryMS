namespace LibraryMS.Core.Entities;

/// <summary>
/// Represents a book category/genre in the library.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BookCount { get; set; } // Computed field
}
