// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Globalization;
using System.Resources;
using System;

namespace BamlLocalization
{
    /// <summary>
    /// Encapsulates <see cref="ResourceManager"/> for the resource string table.
    /// </summary>
    internal static class StringLoader
    {
        /// <summary>
        /// Retrieves the exception string resources <see cref="ResourceManager"/> for current locale, currently only en-US is available.
        /// </summary>
        private static readonly ResourceManager _resourceManager = new("Resources.StringTable", typeof(StringLoader).Assembly);

        /// <summary>
        /// Retrieves the message using <paramref name="id"/> and formats in any <paramref name="args"/> using <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        /// <param name="id">The resource ID to retrieve from resources.</param>
        /// <param name="args">Additional arguments that the message requires.</param>
        /// <returns>The formatted resource message or <see langword="null"/> if the resource was not found.</returns>
        public static string? Get(string id, params ReadOnlySpan<object?> args)
        {
            string? message = _resourceManager.GetString(id);

            return message is not null && !args.IsEmpty ? string.Format(CultureInfo.CurrentCulture, message, args) : message;
        }
    }
}
