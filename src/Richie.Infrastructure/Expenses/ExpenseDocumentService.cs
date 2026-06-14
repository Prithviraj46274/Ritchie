using System.IO;
using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Expenses;
using Richie.Application.Storage;
using Richie.Domain.Assets;
using Richie.Domain.Auditing;
using Richie.Domain.Expenses;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Expenses;

public sealed class ExpenseDocumentService : IExpenseDocumentService
{
    private const string Module = "Expenses";
    private static readonly string ReceiptsFolder = Path.Combine("receipts", "expenses");

    private readonly IAppDbContextFactory _factory;
    private readonly IFileVault _vault;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public ExpenseDocumentService(IAppDbContextFactory factory, IFileVault vault, IUserSession session, IClock clock)
    {
        _factory = factory;
        _vault = vault;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<ExpenseDocumentDto> GetForExpense(Guid expenseId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.ExpenseDocuments.AsNoTracking()
            .Where(d => d.ExpenseId == expenseId && d.UserId == userId)
            .OrderByDescending(d => d.CreatedUtc)
            .Select(d => new ExpenseDocumentDto(d.Id, d.OriginalFileName, d.Kind, d.SizeBytes, d.CreatedUtc))
            .ToList();
    }

    public Guid Attach(Guid expenseId, string originalFileName, byte[] content)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;

        using RichieDbContext db = _factory.Create();
        bool owns = db.Expenses.Any(e => e.Id == expenseId && e.UserId == userId);
        if (!owns)
            throw new InvalidOperationException("Expense not found for the current user.");

        string storedFileName = _vault.Save(content, ReceiptsFolder);
        var document = new ExpenseDocument
        {
            Id = Guid.NewGuid(),
            ExpenseId = expenseId,
            UserId = userId,
            OriginalFileName = Path.GetFileName(originalFileName),
            StoredFileName = storedFileName,
            Kind = DetermineKind(originalFileName),
            SizeBytes = content.LongLength,
            CreatedUtc = now
        };
        db.ExpenseDocuments.Add(document);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(ExpenseDocument), document.Id,
            $"Attached bill '{document.OriginalFileName}'.");
        db.SaveChanges();
        return document.Id;
    }

    public byte[] OpenContent(Guid documentId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        ExpenseDocument doc = db.ExpenseDocuments.AsNoTracking()
            .FirstOrDefault(d => d.Id == documentId && d.UserId == userId)
            ?? throw new InvalidOperationException("Document not found.");
        return _vault.Read(ReceiptsFolder, doc.StoredFileName);
    }

    public bool Delete(Guid documentId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        ExpenseDocument? doc = db.ExpenseDocuments.FirstOrDefault(d => d.Id == documentId && d.UserId == userId);
        if (doc is null)
            return false;

        _vault.Delete(ReceiptsFolder, doc.StoredFileName);
        db.ExpenseDocuments.Remove(doc);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(ExpenseDocument), doc.Id,
            $"Deleted bill '{doc.OriginalFileName}'.");
        db.SaveChanges();
        return true;
    }

    private static DocumentKind DetermineKind(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => DocumentKind.Pdf,
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" => DocumentKind.Image,
        _ => DocumentKind.Other
    };
}
