# KnightCore implementation and verification

27 September 2026. This document describes the delivered MVP, not a production deployment.

| Area | Delivered / verification |
| --- | --- |
| Public storefront | Six segments, configurable packages, included and optional features; Angular production build passed |
| Authentication | ASP.NET Identity cookies, verified email, reset flow, lockout, CSRF and role checks; API tests passed |
| Pricing | Fixed/per-unit Strategy implementations, quantity/dependency validation, decimal server totals; API tests passed |
| Ordering | Versioned quote, transactional order + invoice + job + idempotency records; replay and ownership tests passed |
| Custom scope | Customer request, admin offer, customer acceptance; API tests passed |
| Invoice PDF | PDFsharp rendering, durable retries, stable private storage key, authorized stream download; sample generated |
| Workspace/admin | Order history, allowed status transitions, delivery URL, package version editor, failed-job retry UI |
| SQL Server | EF migration and idempotent SQL script generated; runtime not tested here |
| Mobile | Responsive CSS and a 390px layout harness; native Android/iPhone testing not performed |
| Hosting | Source and Docker evaluation setup supplied; no production service deployed |

## Differences from the target HLD

The detailed HLD remains the target architecture. The source implements a first MVP with these explicit differences:

- One ASP.NET Core application with logical Domain/Application/Infrastructure/API folders, not separately deployed services or separate assemblies per module.
- One EF DbContext. Pricing, quotes, checkout and invoice jobs have dedicated services; cross-module compile-time ownership enforcement is not yet applied.
- Identity uses same-origin HttpOnly cookies. The target's administrator MFA and confirmation resend flow are not yet implemented. Add them before a public production launch.
- Invoice downloads stream through an authorized endpoint (`GET /api/v1/invoices/{id}/download`) rather than returning signed object-storage URLs. This keeps access control server-side.
- Browser drafts persist only selection identifiers and quantities locally. There is a save-configuration API; account-based draft listing/restoration is not yet exposed in the UI.
- Catalog administration versions existing packages through a JSON editor. Creating new segments/packages currently requires a seed/code update. A richer catalog editor is future work.
- API lists use bounded pagination for orders; admin requests/jobs have bounded result limits. Admin order UI currently shows the first 30 records.
- PDF and email jobs share a hosted worker. Separate worker deployment, distributed observability, alerting, retention automation and dedicated operational dashboards remain future deployment work.
- Emails are at-least-once delivery; a crash after provider acceptance can produce a duplicate email. PDF storage and database status updates are idempotent.
- SQL Server migrations are supplied. Local API validation used SQLite; it does not establish SQL Server concurrency, isolation or production capacity behavior.
- Browser preview was read-only because its restricted environment could not run CoreCLR. Full customer checkout was validated through HTTP APIs, not a complete browser interaction test.
- No payment collection, subscription billing, tax-jurisdiction engine, mobile native app, or automatic provisioning of purchased software is included.

## Deployment configuration

Use `ASPNETCORE_ENVIRONMENT=Production`, `Database__Provider=SqlServer`, `ConnectionStrings__Default`, HTTPS `PublicOrigin`, `Email__Provider=Smtp`, `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password` and `Email__From`. Production startup rejects SQLite, local email and non-HTTPS public origins.

Configure `Billing__SellerName`, `Billing__SellerAddress`, `Billing__SellerEmail`, `Billing__SellerTaxId`, `Billing__TaxRate`, `Billing__TermsVersion` and `Billing__Evaluation`. TaxRate is a fraction (0.18 means 18%). Tax handling is a single configured rate, not a full GST/compliance implementation. Keep Evaluation=true until business details and invoice requirements are reviewed.

`InvoiceStorage__Provider=Azure` uses `InvoiceStorage__ConnectionString` and `InvoiceStorage__Container`; otherwise private local storage is used. Persist and encrypt the application data volume, including Data Protection keys. The current code persists keys on disk without certificate-based key encryption; protect that volume and add managed key protection before scale-out.

Apply the supplied migration through a controlled initialization job (`dotnet KnightCore.Api.dll --initialize`). Use a dedicated migration credential and a lower-privilege runtime SQL user. Do not use the Compose SA credential in production. Back up SQL, private invoice objects and encryption keys, and test restoration.

Production release gates: administrator MFA, confirmation resend, SQL Server/SMTP/Azure integration validation, browser checkout and real-device accessibility testing, backup restore, secrets management, HTTPS and operational alerts. These gates are not reported as completed.
