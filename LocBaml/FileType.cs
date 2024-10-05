// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace BamlLocalization
{
    /// <summary>
    /// File type, recognized using extension.
    /// </summary>
    internal enum FileType
    {
        NONE = 0,
        /// <summary>
        /// File is a standalone .baml
        /// </summary>
        BAML = 1,
        /// <summary>
        /// File is a manifest .resources container.
        /// </summary>
        RESOURCES = 2,
        /// <summary>
        /// File is an executable .dll (probably without entrypoint)
        /// </summary>
        DLL = 3,
        /// <summary>
        /// File is a comma-delimited .csv
        /// </summary>
        CSV = 4,
        /// <summary>
        /// File is a tab-delimited .txt
        /// </summary>
        TXT = 5,
        /// <summary>
        /// File is an executable .exe (probably with entrypoint)
        /// </summary>
        EXE = 6,
    }
}
