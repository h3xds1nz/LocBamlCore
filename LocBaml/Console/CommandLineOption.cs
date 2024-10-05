// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System;

namespace BamlLocalization.ConsoleSupport
{
    /// <summary>
    /// Encapsulates single command line option.
    /// </summary>
    internal readonly struct CommandLineOption
    {
        /// <summary>
        /// The name of the argument, mandatory.
        /// </summary>
        public readonly string Name { get; }
        /// <summary>
        /// The value of the argument, optional.
        /// </summary>
        public readonly string? Value { get; }

        public CommandLineOption(string strName, string? strValue)
        {
            ArgumentNullException.ThrowIfNull(strName, nameof(strName));

            Name = strName;
            Value = strValue;
        }
    }
}
