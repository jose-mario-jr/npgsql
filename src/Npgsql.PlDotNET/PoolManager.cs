using System.Collections.Concurrent;

#pragma warning disable CS1591

namespace Npgsql;

/// <inheritdoc />
public static class PoolManager
{

    public static ConcurrentDictionary<string, NpgsqlDataSource> Pools { get; } = new();

    public static void Clear(string connString)
    {
    }

    public static void ClearAll()
    {
    }

    static PoolManager()
    {
    }

    public static void Reset()
    {
    }

}