namespace Richie.Domain.Assets;

public enum DocumentKind
{
    Pdf = 1,
    Image = 2,
    Other = 3
}

/// <summary>
/// A file (PDF/photo/certificate) attached to an asset. The bytes are stored encrypted on disk
/// (PRD §19.3); this record only holds metadata + the encrypted file's name.
/// </summary>
public class AssetDocument
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public DocumentKind Kind { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedUtc { get; set; }
}
