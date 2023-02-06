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
using PlDotNET.Common;

#pragma warning disable CS1591

namespace Npgsql
{
    public class NpgsqlCommand : NpgsqlCommandOrig
    {
        IntPtr _cmdPointer = IntPtr.Zero;
        public NpgsqlConnection InternalConnection { get; private set; }

        public NpgsqlCommand()
        {
            this.InternalConnection = this.InternalConnection ?? new NpgsqlConnection();
            Prepare();
        }

        public NpgsqlCommand(string? cmdText) : this()
        {
            _commandText = cmdText ?? string.Empty;
            InternalConnection = new NpgsqlConnection();
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection? connection) : this(cmdText)
        {
            InternalConnection = connection ?? new NpgsqlConnection();
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection? connection, NpgsqlTransaction? transaction)
            : this(cmdText, connection)
            => Transaction = transaction;

        public override void Prepare() => Prepare(false).GetAwaiter().GetResult();

        Task Prepare(bool async, CancellationToken cancellationToken = default)
        {
            Elog.Info("Prepare SPI statement");
            pldotnet_SPIPrepare(this._commandText, ref this._cmdPointer);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public new Task<NpgsqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
            => ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);


        public new Task<NpgsqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            using (NoSynchronizationContextScope.Enter())
                return ExecuteReader(behavior, async: true, cancellationToken).AsTask();
        }

        public new NpgsqlDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
            => ExecuteReader(behavior, async: false, CancellationToken.None).GetAwaiter().GetResult();

        public new NpgsqlDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => ExecuteReader(behavior);

        public new async ValueTask<NpgsqlDataReader> ExecuteReader(CommandBehavior behavior, bool async, CancellationToken cancellationToken){

            IntPtr cursorPointer = IntPtr.Zero;

            Elog.Info("Open Cursor");
            pldotnet_SPICursorOpen(this._cmdPointer, ref cursorPointer);

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.NpgsqlDataSource), cursorPointer);

            return await Task.FromResult(r);
        }

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPIPrepare(string command, ref IntPtr cmdPointer);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern int pldotnet_SPIExecute(string command, [MarshalAs(UnmanagedType.I1)] bool read_only, long count);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPICursorOpen(IntPtr cmdPointer, ref IntPtr cursorPointer);

    }
}








