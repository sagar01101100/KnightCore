# Verification results

27 September 2026

- ASP.NET Core build: passed, zero warnings/errors.
- Angular production build: passed; initial bundle approximately 299 kB raw / 78 kB estimated transfer.
- Real HTTP API smoke suite: passed.
- Covered: public catalog, server pricing, invalid duplicate features, login enforcement, CSRF rejection, registration and confirmation, cookie authentication, quote/order/invoice creation, idempotent replay, cross-account denial, background PDF generation/download, admin state updates and stale-update rejection, custom quote acceptance, catalog version updates, price snapshot retention, and simultaneous duplicate submissions.
- SQL Server EF migration and idempotent SQL script generation: passed.
- Browser: read-only Angular catalog inspection; responsive viewport inspection.

Not verified: SQL Server runtime/locking, Docker Compose startup, SMTP delivery, Azure storage, a full browser checkout, native mobile devices, load/penetration testing. No production deployment was performed.
