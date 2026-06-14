using Richie.Domain.Assets;

namespace Richie.Application.Expenses;

/// <summary>A bill/receipt attached to an expense.</summary>
public sealed record ExpenseDocumentDto(Guid Id, string FileName, DocumentKind Kind, long SizeBytes, DateTime CreatedUtc);

/// <summary>Attach / list / open / delete encrypted bills &amp; receipts for an expense (PRD §19.3
/// /receipts/expenses). User-scoped and audited.</summary>
public interface IExpenseDocumentService
{
    IReadOnlyList<ExpenseDocumentDto> GetForExpense(Guid expenseId);
    Guid Attach(Guid expenseId, string originalFileName, byte[] content);
    byte[] OpenContent(Guid documentId);
    bool Delete(Guid documentId);
}
