# KnightCore

Responsive Angular storefront and ASP.NET Core modular backend for selling software with configurable features.

## Implemented

- Public six-industry catalog, included features, optional fixed/per-location features, authoritative estimates.
- Verified-email registration, cookie sign-in, password recovery, role authorization and CSRF protection.
- Billing details, expiring versioned quotes, custom requirements and admin-priced offers.
- Atomic order/invoice creation, immutable commercial snapshots, idempotent retries and concurrent-submission protection.
- Background PDF rendering, durable job retries, private invoice downloads, local/SMTP email adapters.
- Customer workspace, delivery history, admin order transitions, package version publishing and failed-job retries.
- Responsive desktop/mobile layout. Angular reactive forms and lazy routes.

## Stack and structure

Angular 22.2 / TypeScript 6, ASP.NET Core 10 / C#, EF Core 10, SQL Server. SQLite is a development/test provider only. PDFsharp generates PDFs. Azure Blob storage is an optional private storage adapter.

`frontend/src/app`: UI, route guards and typed API services.
`backend/KnightCore.Api/Domain`: commercial entities.
`Application`: pricing strategies, quote orchestration, checkout transaction and order lifecycle.
`Infrastructure`: EF, Identity, durable jobs, PDF, storage and email adapters.
`Api`: HTTP contracts and authorization.
`docs`: detailed HLD, implementation differences, diagrams and generated SQL schema.
`tests/smoke.py`: real HTTP end-to-end tests using synthetic data.

The backend is a modular monolith. Interfaces, dependency injection, Strategy and Adapter patterns keep pricing, rendering, email and storage separate. EF Core supplies unit-of-work behavior. This is one deployable backend; it does not introduce premature microservices or an unnecessary repository wrapper around EF.

## Quick local run (SQLite, no Docker required)

Install Node.js 24, .NET SDK 10 and optionally Python 3 for tests.

Terminal 1, from `backend/KnightCore.Api`:

```sh
dotnet restore
dotnet run --environment Development --urls http://localhost:5080
```

Terminal 2, from `frontend`:

```sh
npm ci
npm start
```

Open **http://localhost:4200**. Registration shows a local email-confirmation link in Development. Email messages are also written privately under `backend/KnightCore.Api/data/mailbox`. These are not real emails.

To create an administrator, set `Bootstrap__AdminEmail` and `Bootstrap__AdminPassword` before first API startup. Choose your own unique password; no default account/password is shipped. Bootstrap never promotes an existing customer account. Remove bootstrap secrets after provisioning.

Bash example (values entered locally, not committed):

```sh
export Bootstrap__AdminEmail='your-admin@example.com'
read -s -p 'Admin password: ' Bootstrap__AdminPassword
export Bootstrap__AdminPassword
dotnet run --environment Development --urls http://localhost:5080
```

PowerShell users can set `$env:Bootstrap__AdminEmail` and `$env:Bootstrap__AdminPassword` before the same `dotnet run` command.

## Local SQL Server stack

1. Install Docker Desktop with Linux containers (SQL Server requires a compatible x86-64 host).
2. Copy `.env.example` to `.env`, enter distinct strong passwords and your admin email, and review SQL Server license terms before setting `SQL_ACCEPT_EULA=Y`.
3. Run `docker compose up --build`.
4. Open **http://localhost:8080**.

The initialization job applies EF migrations before the API starts. SQL Server Developer edition and Development mode are for local evaluation. SQL and application data persist in named volumes. Do not delete those volumes if you need the stored orders or keys.

The Compose stack and SQL Server runtime could not be executed in the authoring environment because Docker/SQL Server were unavailable. Migration generation and backend compilation passed; SQL Server integration remains a deployment gate.

## Validation

```sh
dotnet build backend/KnightCore.Api
cd frontend
npm ci
npm run build
cd ..
python3 tests/smoke.py
```

Set `DOTNET` to the executable path if it is not on PATH. Smoke tests start and stop their own API with a temporary SQLite database. They exercise real Identity cookies, CSRF, estimates, quotes, transactions, job processing, PDF output, ownership checks, admin transitions and custom offers. They write only synthetic sample assets under `docs` / `frontend/public`.

## Screenshots and design preview

The captured screen is the real compiled Angular UI with the catalog captured from the test API. The browser preview environment cannot host .NET; therefore `/?preview=design` uses a clearly labeled read-only catalog fixture and disables API operations. It is **not** an online store or evidence of a deployed backend. Normal application mode uses the .NET API exclusively. `/responsive-preview.html` displays the same Angular UI inside a 390px viewport for layout inspection.

For the managed visual preview: `npm run build`, then `npm run dev`. For ordinary development use `npm start`.

## Production readiness

Read `docs/Implementation-Status.md` before deployment. Production needs actual seller/tax/terms configuration, HTTPS hosting, SQL Server credentials with least privilege, SMTP, durable protected key/storage volumes, operational monitoring and backups. Administrator MFA, confirmation resend, production-provider integration testing and a complete browser checkout acceptance test remain outstanding. Do not treat sample invoices as compliant tax invoices. Online payments and automatic delivery of purchased software are outside this MVP.
