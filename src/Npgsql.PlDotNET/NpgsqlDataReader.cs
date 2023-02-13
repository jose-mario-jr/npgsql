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
using PlDotNET.Common;
using PlDotNET.Handler;

#pragma warning disable CS8618, CS8619, CS8604, CS1591

namespace Npgsql
{
    /// <summary>
    /// Represents a data reader for PostgreSQL data.
    /// </summary>
    public class NpgsqlDataReader : NpgsqlDataReaderOrig
    {
        /// <summary>
        /// The number of rows retrieved by the data reader.
        /// </summary>
        private int NRows = 0;

        /// <summary>
        /// The number of columns in the result set.
        /// </summary>
        private int NCols = 0;

        /// <summary>
        /// The number of columns in the result set.
        /// </summary>
        private new int FieldCount;

        /// <summary>
        /// The type of the columns in the result set.
        /// </summary>
        private int[] ColumnTypes;

        /// <summary>
        /// The name of the columns in the result set.
        /// </summary>
        private string[] ColumnNames;

        /// <summary>
        /// The current row of the result set, where each item is a PostgreSQL datum.
        /// </summary>
        private IntPtr[] CurrentRow;

        /// <summary>
        /// The cursor pointer for the data reader.
        /// </summary>
        private IntPtr CursorPointer;

        /// <summary>
        /// Constructor
        /// </summary>
        public NpgsqlDataReader(NpgsqlConnector connector, IntPtr cursorPoint)
        : base(connector)
        {
            CursorPointer = (IntPtr)cursorPoint;
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            using (NoSynchronizationContextScope.Enter())
            {
                var task = Read(true, cancellationToken);
                task.Wait();
                return task;
            }
        }

        async Task<bool> Read(bool async, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Read());
        }

        /// <summary>
        /// Advances the reader to the next row in a result set.
        /// </summary>
        /// <returns><b>true</b> if there are more rows; otherwise <b>false</b>.</returns>
        public override bool Read()
        {
            pldotnet_SPICursorFetch(this.CursorPointer);
            pldotnet_GetTableDimensions(ref this.NRows, ref this.NCols);

            if (this.NRows < 1)
            {
                this.Close();
                return false;
            }

            this.CurrentRow = new IntPtr[this.NCols];

            if (this.ColumnNames == null && this.ColumnTypes == null)
            {
                IntPtr[] columnNamePts = new IntPtr[this.NCols];
                this.ColumnTypes = new int[this.NCols];
                pldotnet_GetColProps(this.ColumnTypes, columnNamePts);
                this.ColumnNames = columnNamePts.ToList().Select(namePts => Marshal.PtrToStringAuto(namePts)).ToArray();
            }

            pldotnet_GetTable(this.CurrentRow);

            this.FieldCount = this.NCols;

            return true;
        }

        /// <summary>
        /// Gets the name of the column, given the zero-based column ordinal.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        public override string GetName(int ordinal)
            => this.ColumnNames[ordinal];

        /// <summary>
        /// Gets the column ordinal given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < this.ColumnNames.Length; i++)
            {
                if (this.ColumnNames[i].ToLower() == name.ToLower())
                {
                    return i;
                }
            }

            throw new ArgumentException("Column not found", nameof(name));
        }

        /// <summary>
        /// Synchronously gets the value of the specified column as a type.
        /// </summary>
        /// <typeparam name="T">Synchronously gets the value of the specified column as a type.</typeparam>
        /// <param name="ordinal">The column to be retrieved.</param>
        /// <returns>The column to be retrieved.</returns>
        public override T GetFieldValue<T>(int ordinal)
            => (T)DatumConversion.InputValue(CurrentRow[ordinal], (OID)ColumnTypes[ordinal]);

        /// <summary>
        /// Synchronously gets the type of the specified column.
        /// </summary>
        /// <param name="ordinal">The column to be retrieved.</param>
        /// <returns>The type of the column.</returns>
        public override Type GetFieldType(int ordinal)
            => typeof(OID);

        /// <summary>
        /// Populates an array of objects with the column values of the current row.
        /// </summary>
        /// <param name="values">An array of Object into which to copy the attribute columns.</param>
        /// <returns>The number of instances of <see cref="object"/> in the array.</returns>
        public override int GetValues(object[] values)
        {
            var count = Math.Min(FieldCount, values.Length);
            for (var i = 0; i < count; i++)
                values[i] = GetValue(i);
            return count;
        }

        /// <summary>
        /// Gets the value of the specified column as an instance of <see cref="object"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override object GetValue(int ordinal)
        {
            return DatumConversion.InputValue(CurrentRow[ordinal], (OID)ColumnTypes[ordinal]);
        }

        // public string getTableLinesMd()
        // {
        //     var sb = new System.Text.StringBuilder();

        //     foreach (DataRow dataRow in this.ReturnedTable.Rows)
        //     {
        //         sb.Append($"| {string.Join(" | ", dataRow.ItemArray)} |\n");
        //     }

        //     return sb.ToString();
        // }

        // public string getTableHeaderMd()
        // {
        //     List<string> columnNamesDT = new ();
        //     var sb = new System.Text.StringBuilder();
        //     var divisor = new System.Text.StringBuilder();

        //     foreach (DataColumn column in this.ReturnedTable.Columns)
        //     {
        //         columnNamesDT.Add(column.ColumnName);
        //         divisor.Append("| - ");
        //     }

        //     divisor.Append("|\n");

        //     sb.Append($"| {string.Join(" | ", columnNamesDT.ToArray())} |\n");
        //     sb.Append(divisor);

        //     return sb.ToString();
        // }

        // public override string ToString()
        // {
        //     _ = new List<string>();

        //     var sb = new System.Text.StringBuilder();
        //     // sb.Append(this.getTableHeaderMd());
        //     // sb.Append(this.getTableLinesMd());

        //     return sb.ToString();
        // }

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPIFinish();

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_GetTableDimensions(ref int nrows, ref int ncols);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_GetTable(IntPtr[] datums);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_GetColProps(int[] columnTypes, IntPtr[] columnNames);

        [DllImport("@PKG_LIBDIR/pldotnet.so")]
        public static extern void pldotnet_SPICursorFetch(IntPtr cursorPointer);

        public override void Close()
        {
            pldotnet_SPIFinish();
        }

        public override ValueTask DisposeAsync()
        {
            using (NoSynchronizationContextScope.Enter())
                return DisposeAsyncCore();

            async ValueTask DisposeAsyncCore()
            {
                Close();
                await Task.Run(() => Elog.Info("Async close ran"));
            }

        }

        protected override void Dispose(bool disposing)
        {
            Close();
        }

        public override bool NextResult(){
            return Read();
        }

        public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Read());
        }

    }
}