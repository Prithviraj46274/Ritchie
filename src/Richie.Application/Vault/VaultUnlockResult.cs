namespace Richie.Application.Vault;

public enum VaultUnlockStatus
{
    Success,
    IncorrectPassword,
    ValidationFailed
}

/// <summary>Outcome of a vault setup/unlock attempt.</summary>
public sealed record VaultUnlockResult(VaultUnlockStatus Status, string? Message = null)
{
    public bool IsSuccess => Status == VaultUnlockStatus.Success;
}
