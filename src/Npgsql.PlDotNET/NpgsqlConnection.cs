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

namespace Npgsql
{

    /// <inheritdoc />
    [System.ComponentModel.DesignerCategory("")]
    public class NpgsqlConnection : NpgsqlConnectionOrig
    {
        NpgsqlDataSource? _dataSource;

        /// <inheritdoc />
        public NpgsqlConnection()
        {
        }

        /// <inheritdoc />
        public NpgsqlConnection(string? connectionString) : this()
            => ConnectionString = connectionString;

        internal static NpgsqlConnection FromDataSource(NpgsqlDataSource dataSource)
        => new()
        {
            _dataSource = dataSource,
        };

        /// <inheritdoc />
        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            using (NoSynchronizationContextScope.Enter())
                return Open(true, cancellationToken);
        }

        internal Task Open(bool async, CancellationToken cancellationToken)
        {
            this._dataSource = NpgsqlDataSource.Create();
            return Task.CompletedTask;
        }

        /// <summary>
        /// DB.
        /// </summary>
        public NpgsqlDataSource NpgsqlDataSource
        {
            get
            {
                if (_dataSource == null)
                {
                    _dataSource = NpgsqlDataSource.Create();
                }
                return _dataSource;
            }
        }

        /// <summary>
        /// Creates and returns a <see cref="NpgsqlCommand"/> object associated with the <see cref="NpgsqlConnectionOrig"/>.
        /// </summary>
        /// <returns>A <see cref="NpgsqlCommand"/> object.</returns>
        public new NpgsqlCommand CreateCommand()
        {
            return new NpgsqlCommand();
        }
    }
}