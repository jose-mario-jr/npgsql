﻿using Npgsql.Internal;
using Npgsql.Util;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Npgsql;

/// <summary>
/// MultiHostDataSourceWrapper class is a wrapper for the NpgsqlMultiHostDataSourceOrig class.
/// This class provides additional functionality specific to working with a multi-host data source.
/// </summary>
public class MultiHostDataSourceWrapper : NpgsqlDataSource
{
    /// <summary>
    /// Indicates whether this data source owns its connectors.
    /// Always returns false for MultiHostDataSourceWrapper
    /// </summary>
    internal override bool OwnsConnectors => false;

    readonly NpgsqlMultiHostDataSourceOrig _wrappedSource;

    /// <summary>
    /// Creates a new instance of MultiHostDataSourceWrapper.
    /// </summary>
    /// <param name="source">An instance of NpgsqlMultiHostDataSourceOrig</param>
    /// <param name="targetSessionAttributes">A target session attribute</param>
    public MultiHostDataSourceWrapper(NpgsqlMultiHostDataSourceOrig source, TargetSessionAttributes targetSessionAttributes)
        : base(CloneSettingsForTargetSessionAttributes(source.Settings, targetSessionAttributes), source.Configuration)
        => _wrappedSource = source;

    /// <summary>
    /// Create a new NpgsqlConnectionStringBuilder instance based on input parameters
    /// </summary>
    /// <param name="settings">An instance of NpgsqlConnectionStringBuilder</param>
    /// <param name="targetSessionAttributes">A target session attribute</param>
    static NpgsqlConnectionStringBuilder CloneSettingsForTargetSessionAttributes(
        NpgsqlConnectionStringBuilder settings,
        TargetSessionAttributes targetSessionAttributes)
    {
        var clonedSettings = settings.Clone();
        clonedSettings.TargetSessionAttributesParsed = targetSessionAttributes;
        return clonedSettings;
    }

    internal override (int Total, int Idle, int Busy) Statistics => _wrappedSource.Statistics;

    internal override void Clear() => _wrappedSource.Clear();
    internal override ValueTask<NpgsqlConnector> Get(NpgsqlConnection conn, NpgsqlTimeout timeout, bool async, CancellationToken cancellationToken)
        => _wrappedSource.Get(conn, timeout, async, cancellationToken);
    internal override bool TryGetIdleConnector([NotNullWhen(true)] out NpgsqlConnector? connector)
        => throw new NpgsqlException("Npgsql bug: trying to get an idle connector from " + nameof(MultiHostDataSourceWrapper));
    internal override ValueTask<NpgsqlConnector?> OpenNewConnector(NpgsqlConnection conn, NpgsqlTimeout timeout, bool async, CancellationToken cancellationToken)
        => throw new NpgsqlException("Npgsql bug: trying to open a new connector from " + nameof(MultiHostDataSourceWrapper));
    internal override void Return(NpgsqlConnector connector)
        => _wrappedSource.Return(connector);

    internal override void AddPendingEnlistedConnector(NpgsqlConnector connector, Transaction transaction)
        => _wrappedSource.AddPendingEnlistedConnector(connector, transaction);
    internal override bool TryRemovePendingEnlistedConnector(NpgsqlConnector connector, Transaction transaction)
        => _wrappedSource.TryRemovePendingEnlistedConnector(connector, transaction);
    internal override bool TryRentEnlistedPending(Transaction transaction, NpgsqlConnection connection,
        [NotNullWhen(true)] out NpgsqlConnector? connector)
        => _wrappedSource.TryRentEnlistedPending(transaction, connection, out connector);
}