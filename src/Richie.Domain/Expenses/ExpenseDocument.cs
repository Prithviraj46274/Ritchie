using Richie.Domain.Assets;

namespace Richie.Domain.Expenses;

/// <summary>An encrypted bill/receipt attached to an expense (mirrors AssetDocument). Files are
/// stored encrypted at rest via the FileVault; only the metadata lives in the DB.</summary>
public class ExpenseDocument
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public DocumentKind Kind { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedUtc { get; set; }
}
