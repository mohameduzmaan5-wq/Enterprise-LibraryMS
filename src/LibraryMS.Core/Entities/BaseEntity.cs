namespace LibraryMS.Core.Entities;

/// <summary>
/// Base entity with common audit fields for all domain models.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
