// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BamlLocalization.Resources
{
    /// <summary>
    /// Enum representing internal states of the reader when reading 
    /// the CSV or tab-separated TXT file
    /// </summary>
    internal enum ResourceReadState
    {
        /// <summary>
        /// State in which the reader is at the start of a column
        /// </summary>
        TokenStart = 0,

        /// <summary>
        /// State in which the reader is reading contents that are quoted
        /// </summary>
        QuotedContent = 1,

        /// <summary>
        /// State in which the reader is reading contents not in quotes
        /// </summary>
        UnQuotedContent = 2,

        /// <summary>
        /// State in which the end of a line is reached
        /// </summary>
        LineEnd = 3
    }
}
