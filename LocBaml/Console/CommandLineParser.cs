// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using BamlLocalization.Data;
using System;

namespace BamlLocalization.ConsoleSupport
{
    /// <summary>
    /// Parser class for command line that works over a set of arguments, providing a custom enumerator.
    /// </summary>
    internal sealed class CommandLineParser
    {
        // Private fields
        private readonly OptionList _validOptions;
        private readonly string[] _argumentList;
        private readonly CommandLineOption[] _optionList;

        private int _argumentIndex;
        private int _optionIndex;

        // Public properties
        public int ArgumentsCount { get => _argumentList.Length; }
        public int OptionsCount { get => _optionList.Length; }

        // Public constructor
        public CommandLineParser(string[] arguments, string[] validOptions)
        {
            // Keep a list of valid option names.
            _validOptions = new OptionList(validOptions);

            // Temporary lists of raw arguments and options and their associated values.
            string[] aArgList = new string[arguments.Length];
            CommandLineOption[] aOptList = new CommandLineOption[arguments.Length];

            // Reset counters of raw arguments and option/value pairs found so far.
            int iArg = 0;
            int iOpt = 0;

            // Iterate through words of command line.
            for (int i = 0; i < arguments.Length; i++)
            {
                string currentArgument = arguments[i];

                // Check for option or raw argument.
                if (currentArgument.StartsWith('/') || currentArgument.StartsWith('-'))
                {
                    ReadOnlySpan<char> optionName;
                    string? optionValue = null;

                    // It's an option. Strip leading '/' or '-' and anything after a value separator (':' or '=').
                    int iColon = currentArgument.AsSpan().IndexOfAny(':', '=');
                    if (iColon == -1)
                        optionName = currentArgument.AsSpan().Slice(1);
                    else
                        optionName = currentArgument.AsSpan().Slice(1, iColon - 1);

                    // Look it up in the table of valid options (to check it exists, get the full option name and to see if an associated value is expected)
                    OptionDefinition option = _validOptions.Lookup(optionName);

                    // Check that the user hasn't specified a value separator for an option that doesn't take a value.
                    if (!option.CanHaveValue && (iColon != -1))
                        throw new ApplicationException(StringTable.Get("Err_NoValueRequired", option.OptionName));

                    // Check that the user has put a colon if the option requires a value.
                    if (option.RequiresValue && (iColon == -1))
                        throw new ApplicationException(StringTable.Get("Err_ValueRequired", option.OptionName));

                    // Go look for a value if there is one.
                    if (option.CanHaveValue && iColon != -1)
                    {
                        if (iColon == (currentArgument.Length - 1))
                        {
                            // No value separator, or
                            // separator is at end of
                            // option; look for value in
                            // next command line arg.
                            if (i + 1 == arguments.Length)
                            {
                                throw new ApplicationException(StringTable.Get("Err_ValueRequired", option.OptionName));
                            }
                            else
                            {
                                if (arguments[i + 1].StartsWith('/') || arguments[i + 1].StartsWith('-'))
                                    throw new ApplicationException(StringTable.Get("Err_ValueRequired", option.OptionName));

                                optionValue = arguments[i + 1];
                                i++;
                            }
                        }
                        else
                        {
                            // Value is in same command line arg as the option, substring it out.
                            optionValue = currentArgument.Substring(iColon + 1);
                        }
                    }

                    // Build the option value pair.
                    aOptList[iOpt++] = new CommandLineOption(option.OptionName, optionValue);
                }
                else
                {
                    // Command line word is a raw argument.
                    aArgList[iArg++] = currentArgument;
                }
            }

            // Allocate the non-temporary arg and option lists at exactly
            // the right size.
            _argumentList = new string[iArg];
            _optionList = new CommandLineOption[iOpt];

            // Copy in the values we've calculated.
            Array.Copy(aArgList, _argumentList, iArg);
            Array.Copy(aOptList, _optionList, iOpt);
        }

        public string? GetNextArg()
        {
            if (_argumentIndex >= _argumentList.Length)
                return null;

            return _argumentList[_argumentIndex++];
        }

        // TODO: Rewrite this into normal enumerator
        public CommandLineOption? GetNextOption()
        {
            if (_optionIndex >= _optionList.Length)
                return null;

            return _optionList[_optionIndex++];
        }
    }
}
