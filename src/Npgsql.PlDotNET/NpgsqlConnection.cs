using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Npgsql.Internal;
using Npgsql.PostgresTypes;
using PlDotNET.Handler;

namespace Npgsql.PlDotNET
{

    /// <inheritdoc />
    [System.ComponentModel.DesignerCategory("")]
    public class NpgsqlConnection : NpgsqlConnectionOrig
    {

        NpgsqlMultiHostDataSource? _dataSource;

        /// <inheritdoc />
        public NpgsqlConnection()
        {
        }

        /// <inheritdoc />
        public NpgsqlConnection(string? connectionString) : this()
        {
        }

        /// <inheritdoc />
        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            using (NoSynchronizationContextScope.Enter())
                return Open(true, cancellationToken);
        }

        internal Task Open(bool async, CancellationToken cancellationToken)
        {
            this._dataSource = NpgsqlMultiHostDataSource.Create();
            return Task.CompletedTask;
        }
    }
}