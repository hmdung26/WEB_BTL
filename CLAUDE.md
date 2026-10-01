# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Current state

This workspace contains only one file, [muctieu.md](muctieu.md) — the authoritative spec (written in Vietnamese) for the project. **No application code exists yet.** The project is at the planning stage.

The spec describes converting an existing Java/Spring Boot 3 + React sales-management system (repo `hmdung26/javaBTL_QLBanHang`) into a **traditional ASP.NET Core 8 MVC** application (Razor Views, server-side rendering) with SQL Server.

When implementing, treat [muctieu.md](muctieu.md) as the source of truth and read it in full before starting. Everything below summarizes its key decisions; the file holds the complete detail (17 entities, permission matrix, per-module controllers, phased plan).

## Intended stack (per spec)

- ASP.NET Core 8 MVC, C# 12, Entity Framework Core (`Microsoft.EntityFrameworkCore.SqlServer`)
- Razor Views (`.cshtml`) with Bootstrap/Tailwind; no React SPA
- ASP.NET Core Identity + **Cookie Authentication** (roles `User`, `Staff`, `Admin`)
- AutoMapper for entity↔ViewModel mapping; FluentValidation or Data Annotations
- Gemini REST API via `HttpClientFactory`, key in `appsettings.json` (`GEMINI_API_KEY`)
- Uploaded files under `wwwroot/uploads/`, served by static-file middleware

## Big-picture architecture

- **Layering:** Controller → Service (interface + implementation, DI) → `DbContext` directly (or optional repository layer) → Entity. The spec explicitly allows Services to use `DbContext` directly instead of a mandatory repository layer.
- **17 entities** map to `Models/` classes with `DbSet<T>` in `Data/AppDbContext.cs`; primary keys are `int` identity. Note `Order` is the table name but the entity is named `SalesOrder` to avoid the `Order` SQL keyword.
- **Two inventory layers** must stay in sync: `Product.StockQuantity` (aggregate) and per-serial `WarehouseItem` records with `Status` (`Available/Reserved/Sold/Damaged/Warranty`). Order placement reserves/sells warehouse items and adjusts stock; cancellation reverses it.
- **Cart is client-side in the original (React Context).** The spec recommends switching to Session or a `Cart`/`CartItem` table in MVC.
- **Auth roles:** `User`, `Staff`, `Admin`, enforced with `[Authorize(Roles = "...")]`. Full per-feature permission matrix is in spec §4. Default admin seeded on first run: `admin` / `admin123`.
- **Dashboard + AI:** `AiController` serves a public Gemini product chatbot and an admin-only AI report that summarizes dashboard numbers in natural language.

## Suggested project layout

Defined in spec §6: `Controllers/` (with an `Admin/` subfolder), `Models/`, `ViewModels/`, `Data/`, `Services/{Interfaces,Implementations}/`, `Views/` (with `Shared/_Layout.cshtml` and `_AdminLayout.cshtml`), `wwwroot/`, `Migrations/`, `Program.cs`, `SalesManagement.csproj`.

## Phased implementation plan

Spec §7 prescribes 8 phases in order: (1) project init + EF Core + 17 entities + first migration + seed data, (2) Identity/auth + role authorization, (3) customer-facing product catalog (Product/Category/Brand/ProductImage/Banner/ProductReview), (4) cart + ordering + promotion + stock deduction, (5) payment + invoice, (6) internal ops (order management, warehouse, warranty, user admin), (7) dashboard + reports + Gemini AI, (8) notifications, file upload, end-to-end testing.

## Notes for when code is scaffolded

No build/lint/test commands exist yet — add them here once the project is created. The expected workflow will be standard .NET CLI: `dotnet build`, `dotnet run`, `dotnet ef migrations add <Name>` / `dotnet ef database update`, and `dotnet test` for a test project (none scaffolded yet).
