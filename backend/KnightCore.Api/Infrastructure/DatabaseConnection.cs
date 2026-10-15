using Microsoft.Data.SqlClient;

namespace KnightCore.Infrastructure;

public static class DatabaseConnection
{
    public static string ResolveSqlServer(IConfiguration configuration)
    {
        // Preserve existing Compose and host-provided connection strings.
        var configured = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var server = configuration["Database:Server"];
        var database = configuration["Database:Name"];
        var username = configuration["Database:Username"];
        var password = configuration["Database:Password"];
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "SQL Server requires ConnectionStrings__Default or Database__Server, Database__Name, " +
                "Database__Username and Database__Password. Configure credentials privately on the API host.");

        return new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = username,
            Password = password,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = false,
            ConnectTimeout = 30,
            PersistSecurityInfo = false
        }.ConnectionString;
    }
}
