namespace Richie.Domain.Assets;

/// <summary>
/// High-level asset categories for grouping and organization.
/// </summary>
public enum AssetCategory
{
    Financial = 1,
    Physical = 2,
    Digital = 3
}

/// <summary>
/// Supported asset types (PRD §6.1). Every type is named explicitly across the app —
/// there is never an "Others" bucket.
/// </summary>
public enum AssetType
{
    MutualFund = 1,
    Equity = 2,
    SovereignGoldBond = 3,
    RealEstate = 4,
    DigitalGold = 5,
    GoldJewellery = 6,
    GuaranteedInvestmentPlan = 7
}

public static class AssetTypeExtensions
{
    public static AssetCategory GetCategory(this AssetType type) => type switch
    {
        AssetType.MutualFund or AssetType.Equity or AssetType.SovereignGoldBond or AssetType.GuaranteedInvestmentPlan => AssetCategory.Financial,
        AssetType.RealEstate or AssetType.GoldJewellery => AssetCategory.Physical,
        AssetType.DigitalGold => AssetCategory.Digital,
        _ => AssetCategory.Financial
    };
}
