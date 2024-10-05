// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System;

namespace BamlLocalization.ConsoleSupport
{
    /// <summary>
    /// Builds list of available <see cref="OptionDefinition"/>s, along with their properties (requires value, can have a value).
    /// </summary>
    internal sealed class OptionList
    {
        // Contains a list of options that are available
        private readonly OptionDefinition[] _commandLineOptions;

        /// <summary>
        /// Constructs a list of <see cref="CommandLineOption"/> from <paramref name="options"/> that can be looked up via <see cref="Lookup(ReadOnlySpan{char})"/>.
        /// </summary>
        /// <param name="options"></param>
        public OptionList(string[] options)
        {
            _commandLineOptions = new OptionDefinition[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                string strOption = options[i];

                // A leading '*' implies the option requires a value (the '*' itself is not stored in the option name)
                if (strOption.StartsWith('*'))
                    _commandLineOptions[i] = new(strOption.Substring(1), requiresValue: true, canHaveValue: true);
                // A leading "+" specifies the option may have a value (the '+' itself is not stored in the option name)
                else if (strOption.StartsWith('+'))
                    _commandLineOptions[i] = new(strOption.Substring(1), requiresValue: false, canHaveValue: true);
                // Doesn't require value and must not have one
                else
                    _commandLineOptions[i] = new(strOption, requiresValue: false, canHaveValue: false);
            }
        }

        public OptionDefinition Lookup(ReadOnlySpan<char> optionName)
        {
            bool bMatched = false;
            int iMatch = -1;

            // Compare option to stored list.
            for (int i = 0; i < _commandLineOptions.Length; i++)
            {
                OptionDefinition option = _commandLineOptions[i];

                // Exact matches always cause immediate termination of the search
                if (optionName.Equals(option.OptionName, StringComparison.OrdinalIgnoreCase))
                    return option;

                // Check for potential match (the input word is a prefix of the current stored option)
                if (option.OptionName.AsSpan().StartsWith(optionName, StringComparison.OrdinalIgnoreCase))
                {
                    // If we've already seen a prefix match then the input word is ambiguous.
                    if (bMatched)
                        throw new ArgumentException(StringLoader.Get("Err_AmbigousOption", optionName.ToString()));

                    // Remember this partial match.
                    bMatched = true;
                    iMatch = i;
                }
            }

            // If we get here with bMatched set, we saw one and only one partial match, so we've got a winner.
            if (bMatched)
                return _commandLineOptions[iMatch];

            // Else the word doesn't match at all.
            throw new ArgumentException(StringLoader.Get("Err_UnknownOption", optionName.ToString()));
        }
    }
}
