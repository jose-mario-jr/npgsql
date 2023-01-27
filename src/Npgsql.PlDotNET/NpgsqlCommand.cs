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
    public class NpgsqlCommand : NpgsqlCommandOrig
    {
        string _commandText;
        IntPtr _cmdPointer;

        internal NpgsqlConnection InternalConnection { get; private set; }

        public NpgsqlCommand()
        {
            this._commandText = this._commandText ?? string.Empty;
            this.InternalConnection = this.InternalConnection  ?? new NpgsqlConnection();

            this.Prepare();
        }
        public NpgsqlCommand(string cmdText, NpgsqlMultiHostDataSource dataSource) : this()
        {
            this._commandText = cmdText;
            this.InternalConnection = new NpgsqlConnection(dataSource);
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection connection)
        {
            this._commandText = cmdText ?? string.Empty;
            this.InternalConnection = connection;
        }

        public NpgsqlCommand(NpgsqlConnection connection)
        {
            this._commandText = string.Empty;
            this.InternalConnection = connection;
        }

        Task Prepare(bool async, CancellationToken cancellationToken = default)
        {
            pldotnet_SPIPrepare(this._commandText, ref this._cmdPointer);

            return Task.CompletedTask;
        }

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPIPrepare(string command, ref IntPtr cmdPointer);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPICursorOpen(IntPtr cmdPointer, ref IntPtr cursorPointer);

        public new NpgsqlDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => this.ExecuteReader();

        public new NpgsqlDataReader ExecuteReader()
        {
            IntPtr cursorPointer = IntPtr.Zero;

            pldotnet_SPICursorOpen(this._cmdPointer, ref cursorPointer);

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.getDataSource()))
            {
                CursorPointer = cursorPointer,
            };

            return r;
        }


        /// <summary>
        /// Execute reader
        /// </summary>
        public new async ValueTask<NpgsqlDataReader> ExecuteReader(CommandBehavior behavior, bool async, CancellationToken cancellationToken)
        {

            IntPtr cursorPointer = IntPtr.Zero;

            pldotnet_SPICursorOpen(this._cmdPointer, ref cursorPointer);

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.getDataSource()))
            {
                CursorPointer = cursorPointer,
            };

            return await Task.FromResult(r);

        }

    }
}