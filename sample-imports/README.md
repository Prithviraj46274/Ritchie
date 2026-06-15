# Sample bulk-import files

Demo CSVs for trying Richie's bulk upload. Each file's header row matches the importer
exactly, so you can import it as-is (or use it as a template). Dates are `yyyy-MM-dd`;
amounts are plain numbers (no ₹ symbol, no thousands separators).

| File | Where to import | Button |
|---|---|---|
| `assets.csv` | Asset Documentation | **Bulk upload** |
| `expenses.csv` | Expense Tracker | **Bulk upload** |
| `income.csv` | Expense Tracker → **Income** | **Bulk upload** |
| `passwords.csv` | Password Vault (unlock the vault first) | **Bulk upload** |

Notes:
- **Passwords:** the vault must be **unlocked** before importing — entries are encrypted on
  ingestion. Duplicates (same Account + User ID) are reported and skipped.
- **Assets:** valid `Type` values are MutualFund, Equity, SovereignGoldBond, RealEstate,
  DigitalGold, GoldJewellery, GuaranteedInvestmentPlan. `InvestmentMode` is `LumpSum` or `Sip`.
  Blank `CurrentValue` defaults to the invested amount.
- **Expenses:** `Category` must be one of the 10 fixed categories (e.g. GroceriesFood,
  HousingUtilities, Transportation, Healthcare, Education, EntertainmentLeisure,
  InsuranceInvestments, DiningRestaurants, PersonalCareClothing, Miscellaneous).
- Each importer can also generate its own blank template from the upload dialog
  (Download CSV / Download Excel).
