using System;
using System.Collections.Generic;
using System.Linq;

namespace Richie.UI.Services;

public interface IVaultRevealStateService
{
    void SetRevealed(Guid id);
    void SetHidden(Guid id);
    void Clear();
    IReadOnlyCollection<Guid> GetRevealedEntryIds();
}

public sealed class VaultRevealStateService : IVaultRevealStateService
{
    private readonly HashSet<Guid> _revealed = new();

    public void SetRevealed(Guid id) => _revealed.Add(id);

    public void SetHidden(Guid id) => _revealed.Remove(id);

    public void Clear() => _revealed.Clear();

    public IReadOnlyCollection<Guid> GetRevealedEntryIds() => _revealed.ToArray();
}
