<div align="center">
  <img src="src/Richie.UI/Assets/richie-logo.png" alt="Richie" width="96" />

  # Richie

  **Your offline-first personal finance companion for Windows.**

  Track assets, expenses, income, an encrypted password vault, and your financial health —
  all on your machine, all encrypted, no cloud, no account, no internet required.
</div>

---

## Overview

Richie is a standalone Windows desktop application for personal financial management. It is
**offline-first and private by design**: every feature works with no internet connection, and your
financial data never leaves your device. The whole database is encrypted at rest, vault passwords
get an extra layer of field-level encryption, and keys are tied to your Windows account.

Beyond raw numbers, Richie is positioned as a **financial-intelligence tool** — dashboards and
module home screens surface conclusions ("your dining spend rose 23% vs last month"), not just
tables.

## Features

- **Dashboard** — a greeting hero, KPI cards (total assets, invested, monthly expenses, P&L,
  financial-health score), actionable cross-module insights, and charts: asset allocation (donut),
  income vs expense (area), investment growth, and expense breakdown.
- **Asset Documentation** — 7 asset types (Mutual Funds, Equity, SGB, Real Estate, Digital Gold,
  Gold Jewellery, Guaranteed Investment Plans) with type-specific fields, allocation breakdown,
  per-asset documents (encrypted), **SIP automation**, and **goal tracking**.
- **Expense Tracker** — a fixed 10-category model, recurring expenses, budgets, analytics
  (category/monthly/yearly charts), receipts, plus an **Income** sub-module.
- **Password Vault** — AES-256-GCM per-field encryption, re-authentication on every access,
  strength meter, password-health audit, clipboard auto-clear, and security-question recovery.
- **Financial Health Audit** — health & risk scores with a transparent factor breakdown and a
  dimensions radar, age-based benchmark comparison, a compliance dashboard, and insurance
  coverage-gap analysis (Insurance is its own module).
- **Reports & Export** — build reports per module → **PDF, PowerPoint, Excel, and CSV**
  (charts rendered as images; unmasked-password export is gated by a master-password re-auth).
- **Bulk upload** — import Assets, Expenses, Income, and Passwords from CSV/Excel
  (sample files in [`sample-imports/`](sample-imports/)).
- **Settings, Notifications, Backup/Restore, Profile, Help** — themes (light/dark/system),
  auto-lock, encrypted local backups, gamified achievements, and an in-app tour.

> **Status colours are consistent app-wide:** green = good, amber = needs attention, red = critical.
> All amounts are shown in Indian Rupees (₹).

## Tech stack

| Layer | Choice |
|---|---|
| Language / runtime | C# on **.NET 10** (`net10.0-windows`) |
| UI | **WPF + WPF-UI** (Windows 11 Fluent design — Mica, NavigationView) |
| Architecture | Clean Architecture + MVVM (CommunityToolkit.Mvvm) |
| Data | EF Core over **SQLite**, file-encrypted with **SQLCipher** (AES-256) |
| Charts | LiveCharts2 | Export | QuestPDF (PDF), DocumentFormat.OpenXml (PPTX), ClosedXML (XLSX) |
| Bulk import | CsvHelper (CSV), ClosedXML (Excel) |
| DI / hosting | Microsoft.Extensions.DependencyInjection + Hosting (background jobs) |
| Logging | Serilog | Installer | WiX v7 (per-user MSI) |

## Security model

- The whole SQLite file is **SQLCipher-encrypted**. The random DB key is protected with **Windows
  DPAPI** (tied to your Windows user).
- The **password vault** adds per-field **AES-256-GCM** encryption; the vault uses a separate master
  password (PBKDF2-derived key, envelope-encrypted) and requires re-authentication on every access.
- Login passwords and security answers are hashed with **Argon2id**.
- Encryption cannot be disabled. Backups are encrypted and unreadable without the app + correct
  credentials. Every create/update/delete is written to an audit log.

> Because your data is encrypted and stays on your device, **no one can recover it for you** — keep
> your passwords/security answers safe and take regular backups.

## Project structure

```
Richie.slnx
├─ src/
│  ├─ Richie.Domain          # entities & enums (platform-neutral)
│  ├─ Richie.Application     # interfaces, DTOs, use-case contracts (platform-neutral)
│  ├─ Richie.Infrastructure  # EF Core, SQLCipher, crypto, services, background jobs
│  └─ Richie.UI              # WPF + WPF-UI app (MVVM views & view-models)
├─ tests/
│  └─ Richie.Infrastructure.Tests
├─ Richie.Installer/         # WiX v7 MSI (Package.wxs, License.rtf)
├─ sample-imports/           # demo CSVs for bulk upload
├─ claude-docs/              # PRD, build plan, working notes
├─ build-installer.ps1       # publish + build the MSI
└─ publish.ps1               # self-contained publish only
```

## Getting started

**Prerequisites:** Windows 10/11, .NET 10 SDK.

```powershell
dotnet restore
dotnet build Richie.slnx
dotnet test  Richie.slnx                      # full test suite
dotnet run   --project src/Richie.UI          # launch the app
```

Database migrations apply automatically at startup. To work with them manually:

```powershell
dotnet ef migrations add <Name> -p src/Richie.Infrastructure -s src/Richie.Infrastructure
dotnet ef database update     -p src/Richie.Infrastructure -s src/Richie.Infrastructure
```

App data (the encrypted database, logs, documents, backups) lives in `%LOCALAPPDATA%\Richie`.

## Building the installer

```powershell
# One-time WiX setup
dotnet tool install --global wix
wix extension add --global WixToolset.UI.wixext
wix eula accept wix7

.\build-installer.ps1                # → dist\Richie-Setup.msi  (per-user, ~85 MB)
```

The MSI installs to `%LOCALAPPDATA%\Programs\Richie` with a Start-Menu shortcut, shows Richie's
license agreement, and on first launch creates the encrypted local workspace.

## Trying bulk upload

Demo files live in [`sample-imports/`](sample-imports/) (`assets.csv`, `expenses.csv`, `income.csv`,
`passwords.csv`). Open the relevant module → **Bulk upload** → pick the file. (Unlock the vault first
before importing passwords.) Each upload dialog can also download a blank CSV/Excel template.

## Notes

- The **Risk Score, Financial Health Score, and age-group benchmarks** ship as transparent **interim
  placeholders** (flagged in the UI and in code) pending finalization — scores and insights are
  informational and are **not financial advice**.
- Optional market-data/NAV APIs are intentionally out of scope; the app stays fully functional
  offline.

## License

See [`Richie.Installer/License.rtf`](Richie.Installer/License.rtf) for the end-user license agreement.
