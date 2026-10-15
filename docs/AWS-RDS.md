# KnightCore on AWS RDS SQL Server

The API is configured for `knightcore-sql-dev.c5yiswkkck0d.ap-southeast-2.rds.amazonaws.com,1433`
and database `KnightCoreDb`. Supply your existing RDS SQL login privately; no additional
development login is required. GitHub stores the source and does not run the API.

## Host configuration

Provision an always-on backend host supporting Docker, such as an EC2 Linux x86-64 instance.
For a public RDS endpoint, allow the backend's fixed outbound IP on TCP 1433 in the RDS security
group. Your laptop's current /32 rule does not authorize a different server. For a backend
in the same VPC, prefer private routing and a rule allowing its security group instead.

1. Clone the repository on the host and install Docker with Compose.
2. Copy `.env.rds.example` to `.env.rds`. Set `Database__Username` and
   `Database__Password` to your existing RDS SQL Server login and password. Use a login
   permitted to create/update the schema; the existing RDS master login can initialize it.
   Restrict access to this file. Compose env files support single-quoted literal values;
   quote passwords containing `$` or `#` according to Docker Compose env-file syntax.
3. Set a real HTTPS `PublicOrigin` and SMTP values in the same file. Production requires
   SMTP and HTTPS. These are application-host settings, not Angular settings or GitHub secrets
   automatically consumed at runtime.
4. If `KnightCoreDb` does not exist, create it in SSMS with `CREATE DATABASE KnightCoreDb;`.
   Existing local/SQLite data is not copied by migrations.
5. Run `docker compose -f compose.rds.yaml up --build -d`.
   The initialization service applies existing EF migrations and seeds the catalog/roles
   before starting the API. Check `docker compose -f compose.rds.yaml logs initialize api`
   for failures; do not share logs containing credentials.
6. Terminate HTTPS on the host's reverse proxy/load balancer and forward the application's
   domain to `127.0.0.1:8080`. This serves Angular and `/api` together for the existing
   session-cookie/CSRF design. Port 8080 is intentionally bound to loopback.
7. Check `/api/v1/health`, then register, confirm email, sign in, create an order, and download
   an invoice. Confirm the corresponding records in SSMS.

The API image installs official AWS RDS CA certificates and split-credential connections
require encryption with certificate validation. Do not disable certificate validation.
Persist and protect the `rds-app-data` volume: it contains authentication keys and invoices.
Configure business/tax details and complete the deployment checks in `Implementation-Status.md`.

For upgrades, back up first, then use the same Compose command; review migrations before
deployment. Initializing concurrently from multiple hosts is unsupported. Delete/recreate
the completed initializer with `docker compose -f compose.rds.yaml rm -f initialize` before
explicitly rerunning initialization if needed.

## Other hosts and configuration precedence

Any .NET 10 backend host can use these environment variables instead of Compose:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=SqlServer
Database__Username=<existing RDS username>
Database__Password=<existing RDS password>
Email__Provider=Smtp
PublicOrigin=https://<your application domain>
```

Also configure SMTP fields shown in `.env.rds.example`. Run `dotnet KnightCore.Api.dll --initialize`
as a release task, then `dotnet KnightCore.Api.dll` as the web process. Install the official RDS
CA chain in that host's certificate trust store if not using the supplied Docker image.

`ConnectionStrings__Default`, if nonempty, takes precedence over split database settings.
Remove an old localhost connection string when using the RDS settings. The Development
profile intentionally retains SQLite for offline local work; override both
`Database__Provider=SqlServer` and `ConnectionStrings__Default=` to use split RDS settings
under Development. Never store a password in appsettings, Angular, or Git.

No live RDS connection is established merely by committing this configuration. A running API,
private credentials, an initialized database, and network access are all required. The database
password is not available to the code authoring environment, so live integration must be
verified after host configuration.
