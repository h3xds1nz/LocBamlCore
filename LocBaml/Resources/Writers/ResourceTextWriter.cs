// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Buffers;
using System.Text;
using System.IO;
using System;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// ResourceTextWriter that writes values to a CSV file or tab-separated TXT file.
    /// </summary>
    internal sealed class ResourceTextWriter : IDisposable
    {
        private readonly SearchValues<char> _delimiters;
        private readonly TextWriter _writer;
        private readonly char _delimiter;

        private bool _firstColumn;

        /// <summary>
        /// Creates a <see cref="ResourceTextWriter"/> for the specified <paramref name="fileType"/>, with a destination specified by <paramref name="output"/>.
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="output"></param>
        internal ResourceTextWriter(FileType fileType, Stream output)
        {
            ArgumentNullException.ThrowIfNull(output, nameof(output));

            _delimiter = LocBamlConst.GetDelimiter(fileType);
            _delimiters = SearchValues.Create('\"', '\r', '\n', _delimiter);

            // Append UTF8 BOM (Byte Order Marker)    
            _writer = new StreamWriter(output, new UTF8Encoding(true));
            _firstColumn = true;
        }

        internal void WriteColumn(string value)
        {
            // NULL means we will write string.Empty
            // TODO: We could just skip this function then
            ReadOnlySpan<char> valueSpan = value;

            // if it contains delimeter, quote, newline, we need to escape them
            if (valueSpan.ContainsAny(_delimiters))
            {
                // make a string builder at the minimum required length;
                StringBuilder builder = new(valueSpan.Length + 2);

                // put in the opening quote
                builder.Append('\"');

                // double quote each quote
                for (int i = 0; i < valueSpan.Length; i++)
                {
                    builder.Append(valueSpan[i]);
                    if (valueSpan[i] == '\"')
                    {
                        builder.Append('\"');
                    }
                }

                // put in the closing quote
                builder.Append('\"');
                // TODO: If we would write here, we avoid this ToString()
                valueSpan = builder.ToString();
            }

            if (!_firstColumn)
            {
                // if we are not the first column, we write delimeter
                // to seperate the new cell from the previous ones.
                _writer.Write(_delimiter);
            }
            else
            {
                _firstColumn = false;
            }

            _writer.Write(valueSpan);
        }

        internal void EndLine()
        {
            // write a new line
            _writer.WriteLine();

            // set first column to true    
            _firstColumn = true;
        }

        // This will be called from IDisposable.Dispose()
        internal void Close() => _writer?.Close();

        // IDisposable interface implementation
        void IDisposable.Dispose() => Close();

    }
}
