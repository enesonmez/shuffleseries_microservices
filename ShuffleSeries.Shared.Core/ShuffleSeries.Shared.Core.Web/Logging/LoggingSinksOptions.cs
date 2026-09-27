using Serilog;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Root options for configuring pluggable Serilog sinks via appsettings.json or HashiCorp Vault.
/// </summary>
public sealed class LoggingSinksOptions
{
    public const string SectionName = "Logging:Sinks";

    public ConsoleSinkOptions Console { get; set; } = new();
    public FileSinkOptions File { get; set; } = new();
    public PostgreSqlSinkOptions PostgreSql { get; set; } = new();
}

public sealed class ConsoleSinkOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseJsonFormat { get; set; } = true;
}

public sealed class FileSinkOptions
{
    public bool Enabled { get; set; } = false;
    public string Path { get; set; } = "logs/log-.json";
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
    public int RetainedFileCountLimit { get; set; } = 31;
    public bool UseJsonFormat { get; set; } = true;
}

public sealed class PostgreSqlSinkOptions
{
    public bool Enabled { get; set; } = false;
    public string? ConnectionString { get; set; }
    public string ConnectionStringKey { get; set; } = "Database:ConnectionString";
    public string TableName { get; set; } = "AppLogs";
    public string SchemaName { get; set; } = "public";
    public bool AutoCreateSqlTable { get; set; } = true;
}
