// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Reflection;
using System;

namespace BamlLocalization.Options
{
    /// <summary>
    /// Base class for options, valid both for generation and parsing.
    /// </summary>
    internal class LocBamlOptions
    {
        internal string Input { get; }
        internal string Output { get; }

        internal bool HasNoLogo { get; }
        internal bool IsVerbose { get; }

        internal FileType InputType { get; }

        internal Assembly[]? Assemblies { get; }

        public LocBamlOptions(LocBamlCreationOptions options)
        {
            Input = options.Input;
            Output = options.Output;

            HasNoLogo = options.HasNoLogo;
            IsVerbose = options.IsVerbose;
            InputType = options.InputType;

            Assemblies = options.Assemblies;
        }

        /// <summary>
        /// Write message line depending on IsVerbose flag
        /// </summary>
        internal void WriteLine(string? message)
        {
            if (IsVerbose)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Write the message depending on IsVerbose flag
        /// </summary>        
        internal void Write(string? message)
        {
            if (IsVerbose)
            {
                Console.Write(message);
            }
        }
    }
}
