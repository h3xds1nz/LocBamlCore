// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

namespace BamlLocalization.ConsoleSupport
{
    /// <summary>
    /// Encapsulates program-defined command line options along with their properties (requires value, can have a value).
    /// </summary>
    internal readonly struct OptionDefinition
    {
        /// <summary>
        /// The option name, without any prefixes or suffixes.
        /// </summary>
        public readonly string OptionName { get; }
        /// <summary>
        /// Modifiedras to whether you require value for this option.
        /// </summary>
        public readonly bool RequiresValue { get; }
        /// <summary>
        /// Specifies whether the option name may be followed by a value.
        /// </summary>
        public readonly bool CanHaveValue { get; }

        /// <summary>
        /// Constructs a new command line option with given parameters.
        /// </summary>
        public OptionDefinition(string optionName, bool requiresValue, bool canHaveValue)
        {
            OptionName = optionName;
            RequiresValue = requiresValue;
            CanHaveValue = canHaveValue;
        }
    }
}
