using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
        public bool isNonQuery;
        public NpgsqlConnection InternalConnection { get; private set; }

        public NpgsqlCommand()
        {
            Elog.Info("Created NpgsqlCommand with void constructor");
            pldotnet_SPIReady();
            this.InternalConnection = new NpgsqlConnection();
        }

        public NpgsqlCommand(string? cmdText) : this()
        {
            Elog.Info($"Created NpgsqlCommand with string constructor: ***{cmdText}***");
            this.CommandText = cmdText ?? string.Empty;
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection? connection) : this(cmdText)
        {
            Elog.Info("Created NpgsqlCommand with string and connection constructor");
            InternalConnection = connection ?? new NpgsqlConnection();
        }

        public NpgsqlCommand(string? cmdText, NpgsqlConnection? connection, NpgsqlTransaction? transaction)
            : this(cmdText, connection)
        {
            Elog.Info("Created NpgsqlCommand with string, connection and transaction constructor");
            Transaction = transaction;
        }

        /// <summary>
        /// Gets or sets the SQL statement or function (stored procedure) to execute at the data source.
        /// </summary>
        /// <value>The SQL statement or function (stored procedure) to execute. The default is an empty string.</value>
        [AllowNull, DefaultValue("")]
        [Category("Data")]
        public override string CommandText
        {
            get => _commandText;
            set
            {
                if (value == null || value == string.Empty){
                    throw new Exception("Null command error!");
                }
                _commandText = value;
                Elog.Info($"Setting query using NpgsqlCommand.CommandText. Value: ***{_commandText}***");
                this.isNonQuery = !_commandText.ToLower().StartsWith("select");
                Elog.Info($"Is non query? {this.isNonQuery}");
            }
        }
        public override void Prepare()
        {
            if (!isNonQuery)
            {
                Elog.Info("Prepare SPI statement");
                pldotnet_SPIPrepare(this._commandText, ref this._cmdPointer);
            }
        }

        /// <inheritdoc />
        public new Task<NpgsqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
            => ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

        public new Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
        {
            using (NoSynchronizationContextScope.Enter())
            {
                var task = ExecuteScalar(true, cancellationToken).AsTask();
                task.Wait();
                return task;
            }
        }

        public new Task<NpgsqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default)
        {
            using (NoSynchronizationContextScope.Enter())
            {
                var task = ExecuteReader(behavior, async: true, cancellationToken).AsTask();
                task.Wait();
                return task;
            }
        }

        public new NpgsqlDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
            => ExecuteReader(behavior, async: false, CancellationToken.None).GetAwaiter().GetResult();

        public new NpgsqlDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => ExecuteReader(behavior);

        public new async ValueTask<NpgsqlDataReader> ExecuteReader(CommandBehavior behavior, bool async, CancellationToken cancellationToken){
            Elog.Info($"Calling NpgsqlCommand.ExecuteReader. Async? {async}");
            IntPtr cursorPointer = IntPtr.Zero;
            if (!isNonQuery)
            {
                // Elog.Info(Parameters.ToString());
                // foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(Parameters))
                // {
                //     string name = descriptor.Name;
                //     object value = descriptor.GetValue(Parameters) ?? "";
                //     Console.WriteLine("{0}={1}", name, value);
                // }
                // Elog.Info("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                // Elog.Info(Parameters.InternalList[0].NpgsqlDbType.ToString());
                // foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(Parameters.InternalList[0].NpgsqlDbType))
                // {
                //     string name = descriptor.Name;
                //     object value = descriptor.GetValue(Parameters.InternalList[0].NpgsqlDbType) ?? "";
                //     Console.WriteLine("{0}={1}", name, value);
                // }
                Prepare();
                Elog.Info("Open Cursor");
                pldotnet_SPICursorOpen(this._cmdPointer, ref cursorPointer);
            }

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.NpgsqlDataSource), cursorPointer);

            return await Task.FromResult(r);
        }

        public override object? ExecuteScalar() => ExecuteScalar(false, CancellationToken.None).GetAwaiter().GetResult();

        async ValueTask<object?> ExecuteScalar(bool async, CancellationToken cancellationToken)
        {
            Elog.Info($"Calling NpgsqlCommand.ExecuteScalar. Async? {async}");

            var reader = await ExecuteReader(CommandBehavior.Default, async, cancellationToken);

            var read = reader.Read();

            var value = read && reader.FieldCount != 0 ? reader.GetValue(0) : null;

            // // Npgsql read the whole result set to trigger any errors
            // while (async ? await reader.NextResultAsync(cancellationToken) : reader.NextResult()) ;

            return value;
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            Elog.Info($"Calling NpgsqlCommand.ExecuteNonQueryAsync.");
            using (NoSynchronizationContextScope.Enter())
            {
                var task = ExecuteNonQuery(true, cancellationToken);
                task.Wait();
                return task;
            }
        }

        public override int ExecuteNonQuery() => ExecuteNonQuery(false, CancellationToken.None).GetAwaiter().GetResult();

        async Task<int> ExecuteNonQuery(bool async, CancellationToken cancellationToken)
        {
            Elog.Info($"Calling NpgsqlCommand.ExecuteNonQuery.");
            var reader = await ExecuteReader(CommandBehavior.Default, async, cancellationToken);
            try
            {
                pldotnet_SPIReady();
                Elog.Info("Calling pldotnet_SPIExecute...");
                pldotnet_SPIExecute(_commandText, false, 0);
                Elog.Info($"Returning reader.RecordsAffected = {reader.RecordsAffected}");
                return reader.RecordsAffected;
            }
            finally
            {
                Elog.Info($"Accessing finally statement of NpgsqlCommand.ExecuteNonQuery");
                if (async)
               {     Elog.Info($"Calling reader.DisposeAsync()");
                    await reader.DisposeAsync();}
                else
              {      Elog.Info($"Calling reader.Dispose()");
                    reader.Dispose();}
            }
        }

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool pldotnet_SPIReady();

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPIPrepare(string command, ref IntPtr cmdPointer);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern int pldotnet_SPIExecute(string command, [MarshalAs(UnmanagedType.I1)] bool read_only, long count);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPICursorOpen(IntPtr cmdPointer, ref IntPtr cursorPointer);

    }
}








