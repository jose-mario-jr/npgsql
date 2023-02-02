namespace Npgsql;

/// <inheritdoc />
public class NpgsqlMultiHostDataSource : NpgsqlDataSource
{
    internal NpgsqlMultiHostDataSource(NpgsqlConnectionStringBuilder settings, NpgsqlDataSourceConfiguration dataSourceConfig)
    : base(settings, dataSourceConfig)
    {
    }

    /// <inheritdoc />
    public new NpgsqlMultiHostDataSource WithTargetSession(TargetSessionAttributes targetSessionAttributes)
    => (NpgsqlMultiHostDataSource) Create();
}

