namespace LibraryMS.Core.Entities;

/// <summary>
/// Represents a book in the library catalog.
/// </summary>
public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int AvailableQuantity { get; set; } = 1;

    /// <summary>
    /// Returns availability status text.
    /// </summary>
    public string AvailabilityStatus =>
        AvailableQuantity > 0 ? "Available" : "Checked Out";
}
