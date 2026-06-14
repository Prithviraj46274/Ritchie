namespace Richie.Application.Assets;

/// <summary>
/// Canonical column order for the bulk-upload template and parser (PRD §6.4). One universal
/// template covers all asset types; type-specific columns are optional per row.
/// </summary>
public static class AssetImportColumns
{
    public const string Type = "Type";
    public const string Name = "Name";
    public const string Identifier = "Identifier";
    public const string InvestmentStartDate = "InvestmentStartDate";
    public const string InvestedAmount = "InvestedAmount";
    public const string Quantity = "Quantity";
    public const string PurchasePricePerUnit = "PurchasePricePerUnit";
    public const string CurrentValue = "CurrentValue";
    public const string ValuationDate = "ValuationDate";
    public const string InvestmentMode = "InvestmentMode";
    public const string Notes = "Notes";
    public const string Exchange = "Exchange";
    public const string IssuePrice = "IssuePrice";
    public const string MaturityDate = "MaturityDate";
    public const string PlatformName = "PlatformName";
    public const string PropertyAddress = "PropertyAddress";
    public const string AreaSquareFeet = "AreaSquareFeet";
    public const string Weight = "Weight";
    public const string Purity = "Purity";
    public const string AppraiserName = "AppraiserName";
    public const string PolicyNumber = "PolicyNumber";
    public const string GuaranteedReturnPercent = "GuaranteedReturnPercent";

    public static readonly IReadOnlyList<string> All =
    [
        Type, Name, Identifier, InvestmentStartDate, InvestedAmount, Quantity, PurchasePricePerUnit,
        CurrentValue, ValuationDate, InvestmentMode, Notes, Exchange, IssuePrice, MaturityDate,
        PlatformName, PropertyAddress, AreaSquareFeet, Weight, Purity, AppraiserName, PolicyNumber,
        GuaranteedReturnPercent
    ];
}
