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
        IntPtr _cmdPointer = IntPtr.Zero;
        public NpgsqlConnection InternalConnection { get; private set; }

        public NpgsqlCommand()
        {
            this.InternalConnection = this.InternalConnection ?? new NpgsqlConnection();
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection? connection)
        {
            _commandText = cmdText ?? string.Empty;
            InternalConnection = connection ?? new NpgsqlConnection();
        }

        public new Task<NpgsqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            using (NoSynchronizationContextScope.Enter())
                return ExecuteReader(behavior, async: true, cancellationToken).AsTask();
        }

        public new async ValueTask<NpgsqlDataReader> ExecuteReader(CommandBehavior behavior, bool async, CancellationToken cancellationToken){

            IntPtr cursorPointer = IntPtr.Zero;

            // pldotnet_SPICursorOpen(this._cmdPointer, ref cursorPointer);

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.getDataSource()))
            {
                CursorPointer = cursorPointer,
            };

            return await Task.FromResult(r);
        }

    }
}








