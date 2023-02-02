
namespace Npgsql;

/// <inheritdoc />
public class NpgsqlDataSourceBuilder : NpgsqlDataSourceBuilderOrig
{
    /// <inheritdoc />
    public NpgsqlDataSourceBuilder(string? connectionString = null)
    {
    }

    /// <inheritdoc />
    public new NpgsqlDataSource Build()
        => NpgsqlDataSource.Create();

    /// <inheritdoc />
    public new NpgsqlDataSource BuildMultiHost()
        => Build();

    /// <inheritdoc />
    public NpgsqlDataSource WithTargetSession(TargetSessionAttributes targetSessionAttributes)
        => Build();

}
