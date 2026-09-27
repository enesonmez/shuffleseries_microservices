using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace ShuffleSeries.Shared.Core.Web.Logging;

/// <summary>
/// Configures Serilog PostgreSQL sink to persist structured logs directly to a PostgreSQL database table.
/// </summary>
public sealed class PostgreSqlSinkConfigurator : ILogSinkConfigurator
{
    public void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sinksOptions = configuration.GetSection(LoggingSinksOptions.SectionName).Get<LoggingSinksOptions>()
                           ?? new LoggingSinksOptions();

        if (!sinksOptions.PostgreSql.Enabled)
        {
            return;
        }

        var connectionString = sinksOptions.PostgreSql.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(sinksOptions.PostgreSql.ConnectionStringKey))
        {
            connectionString = configuration[sinksOptions.PostgreSql.ConnectionStringKey];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        IDictionary<string, ColumnWriterBase> columnOptions = new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
            { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            { "time_stamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
            { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
            { "log_event", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) }
        };

        loggerConfiguration.WriteTo.Async(a => a.PostgreSQL(
            connectionString: connectionString,
            tableName: sinksOptions.PostgreSql.TableName,
            columnOptions: columnOptions,
            schemaName: sinksOptions.PostgreSql.SchemaName,
            needAutoCreateTable: sinksOptions.PostgreSql.AutoCreateSqlTable));
    }
}
