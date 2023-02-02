using System;
using System.Threading.Tasks;

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
    public new NpgsqlMultiHostDataSource BuildMultiHost()
        => (NpgsqlMultiHostDataSource) NpgsqlDataSource.Create();

    /// <inheritdoc />
    public NpgsqlDataSource WithTargetSession(TargetSessionAttributes targetSessionAttributes)
        => Build();

    /// <summary>
    /// Register a connection initializer, which allows executing arbitrary commands when a physical database connection is first opened.
    /// </summary>
    public NpgsqlDataSourceBuilder UsePhysicalConnectionInitializer(
        Action<NpgsqlConnection>? connectionInitializer,
        Func<NpgsqlConnection, Task>? connectionInitializerAsync)
    {
        return this;
    }
}
