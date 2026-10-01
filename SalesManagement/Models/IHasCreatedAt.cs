namespace SalesManagement.Models;

/// <summary>
/// Entities implementing this interface have their <see cref="CreatedAt"/> set automatically
/// by <c>AppDbContext</c> when first added (equivalent to <c>@PrePersist</c> in the original).
/// </summary>
public interface IHasCreatedAt
{
    DateTime CreatedAt { get; set; }
}
