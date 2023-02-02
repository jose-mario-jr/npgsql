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

#pragma warning disable CS1591

namespace Npgsql
{
    public class NpgsqlDataSource : NpgsqlMultiHostDataSourceOrig
    {
	    // We don't use the connection string, so we use dummy
	    // values for when Npgsql wants to look at it.
        public static string Cs = "Host=localhost;Username=postgres;Password=postgres;Database=postgres";

        public static NpgsqlDataSourceBuilderOrig Dsb = new NpgsqlDataSourceBuilderOrig(Cs);
        public static NpgsqlConnectionStringBuilder Sts = new NpgsqlConnectionStringBuilder(Cs);

        internal NpgsqlDataSource(NpgsqlConnectionStringBuilder settings, NpgsqlDataSourceConfiguration dataSourceConfig)
        : base(settings, dataSourceConfig)
        {
        }

        public static new NpgsqlDataSource Create(string connectionString = "")
        {
            pldotnet_SPIReady();
            return new NpgsqlDataSource(Sts, Dsb.PrepareConfiguration());
        }

        /// <inheritdoc />
        public new NpgsqlConnection CreateConnection()
            => NpgsqlConnection.FromDataSource(this);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool pldotnet_SPIReady();

        // public new NpgsqlCommand CreateCommand(string query)
        //     => new (query);

        /// <inheritdoc />
        public new NpgsqlConnection OpenConnection()
        {
            var connection = this.CreateConnection();

            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }


        /// <inheritdoc />
        public new async ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = this.CreateConnection();

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return (NpgsqlConnection)connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
