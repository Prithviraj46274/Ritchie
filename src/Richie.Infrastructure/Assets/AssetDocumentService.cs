using System.IO;
using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Assets;
using Richie.Application.Authentication;
using Richie.Application.Storage;
using Richie.Domain.Assets;
using Richie.Domain.Auditing;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Assets;

public sealed class AssetDocumentService : IAssetDocumentService
{
    private const string Module = "Assets";
    private static readonly string PhotosFolder = Path.Combine("photos", "assets");
    private static readonly string DocumentsFolder = Path.Combine("documents", "assets");

    private readonly IAppDbContextFactory _factory;
    private readonly IFileVault _vault;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public AssetDocumentService(IAppDbContextFactory factory, IFileVault vault, IUserSession session, IClock clock)
    {
        _factory = factory;
        _vault = vault;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<AssetDocumentDto> GetForAsset(Guid assetId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.AssetDocuments.AsNoTracking()
            .Where(d => d.AssetId == assetId && d.UserId == userId)
            .OrderByDescending(d => d.CreatedUtc)
            .Select(d => new AssetDocumentDto(d.Id, d.OriginalFileName, d.Kind, d.SizeBytes, d.CreatedUtc))
            .ToList();
    }

    public Guid Attach(Guid assetId, string originalFileName, byte[] content)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;

        using RichieDbContext db = _factory.Create();
        bool owns = db.Assets.Any(a => a.Id == assetId && a.UserId == userId);
        if (!owns)
            throw new InvalidOperationException("Asset not found for the current user.");

        DocumentKind kind = DetermineKind(originalFileName);
        string subfolder = Subfolder(kind);
        string storedFileName = _vault.Save(content, subfolder);

        var document = new AssetDocument
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            UserId = userId,
            OriginalFileName = Path.GetFileName(originalFileName),
            StoredFileName = storedFileName,
            Kind = kind,
            SizeBytes = content.LongLength,
            CreatedUtc = now
        };
        db.AssetDocuments.Add(document);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(AssetDocument), document.Id,
            $"Attached '{document.OriginalFileName}'.");
        db.SaveChanges();
        return document.Id;
    }

    public byte[] OpenContent(Guid documentId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        AssetDocument doc = db.AssetDocuments.AsNoTracking()
            .FirstOrDefault(d => d.Id == documentId && d.UserId == userId)
            ?? throw new InvalidOperationException("Document not found.");

        return _vault.Read(Subfolder(doc.Kind), doc.StoredFileName);
    }

    public bool Delete(Guid documentId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        AssetDocument? doc = db.AssetDocuments.FirstOrDefault(d => d.Id == documentId && d.UserId == userId);
        if (doc is null)
            return false;

        _vault.Delete(Subfolder(doc.Kind), doc.StoredFileName);
        db.AssetDocuments.Remove(doc);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(AssetDocument), doc.Id,
            $"Deleted '{doc.OriginalFileName}'.");
        db.SaveChanges();
        return true;
    }

    private static string Subfolder(DocumentKind kind) => kind == DocumentKind.Image ? PhotosFolder : DocumentsFolder;

    private static DocumentKind DetermineKind(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => DocumentKind.Pdf,
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" => DocumentKind.Image,
        _ => DocumentKind.Other
    };
}
