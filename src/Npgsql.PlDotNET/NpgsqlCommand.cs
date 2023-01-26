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

namespace Npgsql.PlDotNET
{
    public class NpgsqlCommand : NpgsqlCommandOrig
    {
        string _commandText;
        private readonly IntPtr cmdPointer;

        internal NpgsqlConnection InternalConnection { get; private set; }

        public NpgsqlCommand(string query, NpgsqlMultiHostDataSource dataSource)
        {
            this._commandText = query;
            this.InternalConnection = new NpgsqlConnection(dataSource);

            pldotnet_SPIPrepare(this._commandText, ref this.cmdPointer);
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection connection)
        {
            this._commandText = cmdText ?? string.Empty;
            this.InternalConnection = connection;
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

            pldotnet_SPICursorOpen(this.cmdPointer, ref cursorPointer);

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.DataSource))
            {
                CursorPointer = cursorPointer,
            };

            return r;
        }
    }
}