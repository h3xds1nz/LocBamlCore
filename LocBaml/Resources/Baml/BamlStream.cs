// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Diagnostics;
using System.IO;
using System;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// BamlStream class which represents a baml stream
    /// </summary>
    internal sealed class BamlStream
    {
        private readonly string _name;
        private readonly Stream _stream;

        /// <summary>
        /// constructor
        /// </summary>
        internal BamlStream(string name, Stream stream)
        {
            _name = name;
            _stream = stream;
        }

        /// <summary>
        /// Name of the baml file
        /// </summary>
        internal string Name
        {
            get => _name;
        }

        /// <summary>
        /// The actual Baml stream
        /// </summary>
        internal Stream Stream
        {
            get => _stream;
        }

        /// <summary>
        /// close the stream
        /// </summary>
        internal void Close()
        {
            _stream?.Close();
        }

        /// <summary>
        /// Helper method which determines whether a stream name and value pair indicates a baml stream
        /// </summary>
        internal static bool IsResourceEntryBamlStream(string name, object value)
        {
            ReadOnlySpan<char> extension = Path.GetExtension(name.AsSpan());
            if (extension.Equals($".{nameof(FileType.BAML)}", StringComparison.OrdinalIgnoreCase))
            {
                // It has .Baml at the end
                Type type = value.GetType();

                if (typeof(Stream).IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Combine baml stream name and resource name to uniquely identify a baml within a 
        /// localization project
        /// </summary>
        internal static string CombineBamlStreamName(string resource, string bamlName)
        {
            Debug.Assert(resource != null && bamlName != null, "Resource name and baml name can't be null");

            ReadOnlySpan<char> suffix = Path.GetFileName(bamlName.AsSpan());
            ReadOnlySpan<char> prefix = Path.GetFileName(resource.AsSpan());

            return $"{prefix}{LocBamlConst.BamlAndResourceSeperator}{suffix}";
        }
    }
}
