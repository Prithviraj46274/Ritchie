using Richie.Domain.Assets;

namespace Richie.Application.Assets;

public sealed record AssetDocumentDto(
    Guid Id, string FileName, DocumentKind Kind, long SizeBytes, DateTime CreatedUtc);

/// <summary>
/// Files attached to an asset, stored encrypted at rest (PRD §6.5/§6.6/§19.3).
/// </summary>
public interface IAssetDocumentService
{
    IReadOnlyList<AssetDocumentDto> GetForAsset(Guid assetId);

    /// <summary>Encrypts and stores the file, returns the new document id.</summary>
    Guid Attach(Guid assetId, string originalFileName, byte[] content);

    /// <summary>Decrypts and returns the file bytes (for preview / download).</summary>
    byte[] OpenContent(Guid documentId);

    bool Delete(Guid documentId);
}
