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
using NpgsqlTypes;

#pragma warning disable CS1591, CS8604

namespace Npgsql
{
    public static class NpgsqlDbTypeExtensions
    {
        public static BuiltInPostgresType GetPostgresTypeInfo(this NpgsqlDbType npgsqlDbType)
        {
            var type = typeof(NpgsqlDbType);
            var memInfo = type.GetMember(npgsqlDbType.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(BuiltInPostgresType), false);

            return (BuiltInPostgresType)attributes[0];
        }
    }

    public class NpgsqlCommand : NpgsqlCommandOrig
    {
        public static Dictionary<string, uint> RangeArrays =
                       new()
        {
            { "int4range", 3905 },
            { "numrange", 3907 },
            { "tsrange", 3909 },
            { "tstzrange", 3911 },
            { "daterange", 3913 },
            { "int8range", 3927 },
        };

        public static Dictionary<string, uint> MultirangeArrays =
                       new()
        {
            { "int4multirange", 6150 },
            { "nummultirange", 6151 },
            { "tsmultirange", 6152 },
            { "tstzmultirange", 6153 },
            { "datemultirange", 6155 },
            { "int8multirange", 6157 },
        };

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
                if (value == null || value == string.Empty)
                {
                    throw new Exception("Null command error!");
                }
                _commandText = value;
                // Elog.Info($"Setting query using NpgsqlCommand.CommandText. Value: ***{_commandText}***");
                this.isNonQuery = !_commandText.ToLower().StartsWith("select");
                // Elog.Info($"Is non query? {this.isNonQuery}");
            }
        }
        public override void Prepare()
        {
            Elog.Info("Prepare() called...");
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

        public new async ValueTask<NpgsqlDataReader> ExecuteReader(CommandBehavior behavior, bool async, CancellationToken cancellationToken)
        {
            Elog.Info($"Calling NpgsqlCommand.ExecuteReader. Async? {async}");
            IntPtr cursorPointer = IntPtr.Zero;
            if (!isNonQuery)
            {
                uint[] paramTypesOid = new uint[Parameters.Count];
                IntPtr[] paramValues = new IntPtr[Parameters.Count];

                if (Parameters.Count > 0)
                {
                    if (string.IsNullOrEmpty(Parameters[0].ParameterName))
                    {
                        for (int i = 0; i < Parameters.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(Parameters[i].ParameterName))
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Parameters.Count; i++)
                        {
                            string parameterName = Parameters[i].ParameterName;
                            if (string.IsNullOrEmpty(parameterName))
                            {
                                throw new NotSupportedException();
                            }
                            if (i < Parameters.Count - 1)
                            {
                                this._commandText = this._commandText.Replace(parameterName.StartsWith("@") ? $"{parameterName}," : $"@{parameterName},", $"${i + 1},");
                            }
                            else
                            {
                                this._commandText = this._commandText.Replace(parameterName.StartsWith("@") ? $"{parameterName}" : $"@{parameterName}", $"${i + 1}");
                            }
                        }
                    }

                    for (int i = 0; i < Parameters.Count; i++)
                    {
                        Elog.Info($"Parameter {i}");
                        Elog.Info(Parameters[i].Value?.ToString());
                        Elog.Info(Parameters[i].NpgsqlDbType.ToString());

                        paramTypesOid[i] = FindOid(Parameters[i].NpgsqlDbType);
                        paramValues[i] = DatumConversion.OutputNullableValue((OID)paramTypesOid[i], Parameters[i].Value);
                    }
                }

                Elog.Info("Open Cursor");
                pldotnet_SPIPrepare(ref this._cmdPointer, this._commandText, Parameters.Count, paramTypesOid);
                pldotnet_SPICursorOpen(ref cursorPointer, this._cmdPointer, paramValues);
            }

            var r = new NpgsqlDataReader(new NpgsqlConnector(this.InternalConnection.NpgsqlDataSource), cursorPointer);

            return await Task.FromResult(r);

            uint FindOid(NpgsqlDbType type)
            {
                int array = (int)NpgsqlDbType.Array; // -2,147,483,648
                int multiRange = (int)NpgsqlDbType.Multirange; // 536,870,921
                int range = (int)NpgsqlDbType.Range; // 1,073,741,824

                int typeValue = (int)type;

                if (typeValue > range)
                {
                    // it is a range!
                    Elog.Info($"Range of {(NpgsqlDbType)(typeValue - range)}.");
                    return ((NpgsqlDbType)(typeValue - range)).GetPostgresTypeInfo().RangeOID;
                }
                else if (typeValue > multiRange)
                {
                    // it is a multirange!
                    Elog.Info($"Multirange of {(NpgsqlDbType)(typeValue - multiRange)}.");
                    return ((NpgsqlDbType)(typeValue - multiRange)).GetPostgresTypeInfo().MultirangeOID;
                }
                else if (typeValue > 0)
                {
                    // it is a base!
                    Elog.Info($"Just {(NpgsqlDbType)(typeValue)}.");
                    return ((NpgsqlDbType)(typeValue)).GetPostgresTypeInfo().BaseOID;
                }
                else
                {
                    // it is an array!

                    int arrayAux = typeValue - array;

                    if (arrayAux > range)
                    {
                        // it is an array of range!
                        Elog.Info($"Array of {(NpgsqlDbType)(arrayAux - range)} range.");
                        // TODO: resolve CS8604 in these lines!
                        return RangeArrays[((NpgsqlDbType)(arrayAux - range)).GetPostgresTypeInfo().RangeName];
                    }
                    else if (arrayAux > multiRange)
                    {
                        // it is an array of multirange!
                        Elog.Info($"Array of {(NpgsqlDbType)(arrayAux - multiRange)} multirange.");
                        return MultirangeArrays[((NpgsqlDbType)(arrayAux - multiRange)).GetPostgresTypeInfo().MultirangeName];
                    }

                    // It is an array of base!
                    Elog.Info($"Array of {(NpgsqlDbType)(typeValue - array)}.");
                    return ((NpgsqlDbType)(arrayAux)).GetPostgresTypeInfo().ArrayOID;
                }
            }
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
                {
                    Elog.Info($"Calling reader.DisposeAsync()");
                    await reader.DisposeAsync();
                }
                else
                {
                    Elog.Info($"Calling reader.Dispose()");
                    reader.Dispose();
                }
            }
        }

        public new void Unprepare()
            => Elog.Info("Unprepare called...");

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool pldotnet_SPIReady();

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPIPrepare(ref IntPtr cmdPointer, string command, int nargs, uint[] paramTypesOid);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern int pldotnet_SPIExecute(string command, [MarshalAs(UnmanagedType.I1)] bool read_only, long count);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPICursorOpen(ref IntPtr cursorPointer, IntPtr cmdPointer, IntPtr[] paramValues);

    }
}








