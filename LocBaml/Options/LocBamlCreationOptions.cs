// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using BamlLocalization.Data;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.IO;
using System;

namespace BamlLocalization.Options
{
    /// <summary>
    /// Very ugly structure that verifies the logic for command-line options.
    /// </summary>
    public struct LocBamlCreationOptions
    {
        public string Input { get; set; }
        public string Output { get; set; }

        internal CultureInfo CultureInfo { get; set; }
        internal string? Translations { get; set; }
        internal bool ToParse { get; set; }
        internal bool ToGenerate { get; set; }
        internal bool HasNoLogo { get; set; }
        internal bool IsVerbose { get; set; }
        internal FileType TranslationFileType { get; set; }
        internal FileType InputType { get; set; }
        internal List<string>? AssemblyPaths { get; set; }
        internal Assembly[]? Assemblies { get; set; }

        /// <summary>
        /// Verifies that the options set are consistent and it's okay to create <see cref="LocBamlOptions"/>.
        /// </summary>
        /// <returns>Outputs an error message in case there was one, otherwise <see langword="null"/>.</returns>
        public string? VerifyOptions()
        {
            // Rule #1: One and only one action at a time
            // i.e. Can't parse and generate at the same time; must do only one of them
            if ((ToParse && ToGenerate) || (!ToParse && !ToGenerate))
                return StringTable.Get("MustChooseOneAction");

            // Rule #2: Must have an input 
            if (string.IsNullOrEmpty(Input))
                return StringTable.Get("InputFileRequired");

            // Rule #2.1: Input must exist
            if (!File.Exists(Input))
                return StringTable.Get("FileNotFound", Input);

            // Rule #2.2: Input must have a valid file type
            if (!TryGetFileType(Input, out FileType? inputType))
                return StringTable.Get("FileTypeNotSupported", Path.GetExtension(Input));

            // Assign valid file type
            InputType = inputType.Value;

            if (ToGenerate)
            {
                // Rule #3: before generation, we must have Culture string - unless we generating from baml
                if (CultureInfo is null && InputType != FileType.BAML)
                    return StringTable.Get("CultureNameNeeded", InputType.ToString());

                // Rule #4: before generation, we must have translation file
                if (string.IsNullOrEmpty(Translations))
                    return StringTable.Get("TranslationNeeded");

                // Rule #4.1: Translation file must exist
                if (!File.Exists(Translations))
                    return StringTable.Get("TranslationNotFound", Translations);

                ReadOnlySpan<char> transExtension = Path.GetExtension(Translations.AsSpan());
                if (transExtension.Equals($".{nameof(FileType.CSV)}", StringComparison.OrdinalIgnoreCase))
                {
                    TranslationFileType = FileType.CSV;
                }
                else
                {
                    TranslationFileType = FileType.TXT;
                }
            }

            // Rule #5: If the output file name is empty, we act accordingly
            if (string.IsNullOrEmpty(Output))
            {
                // Rule #5.1: If it is parse, we default to [input file name].csv
                if (ToParse)
                {
                    ReadOnlySpan<char> fileName = Path.GetFileNameWithoutExtension(Input.AsSpan());
                    Output = $"{fileName}.{nameof(FileType.CSV)}";
                    TranslationFileType = FileType.CSV;
                }
                else
                {
                    // Rule #5.2: If it is generating, and the output can't be empty
                    return StringTable.Get("OutputDirectoryNeeded");
                }
            }
            else
            {
                // output isn't null, we will determind the Output file type                
                // Rule #6: if it is parsing. It will be .csv or .txt.
                if (ToParse)
                {
                    string fileName;
                    string? outputDir;

                    if (Directory.Exists(Output))
                    {
                        // the output is actually a directory name
                        fileName = string.Empty;
                        outputDir = Output;
                    }
                    else
                    {
                        // get the extension
                        fileName = Path.GetFileName(Output);
                        outputDir = Path.GetDirectoryName(Output);
                    }

                    // Rule #6.1: if it is just the output directory
                    // we append the input file name as the output + csv as default
                    if (string.IsNullOrEmpty(fileName))
                    {
                        TranslationFileType = FileType.CSV;
                        Output = $"{outputDir}{Path.DirectorySeparatorChar}{Path.GetFileName(Input.AsSpan())}.{TranslationFileType}";
                    }
                    else
                    {
                        // Rule #6.2: if we have file name, check the extension.
                        ReadOnlySpan<char> outputExtension = Path.GetExtension(Output.AsSpan());

                        // ignore case and invariant culture
                        if (outputExtension.Equals($".{nameof(FileType.CSV)}", StringComparison.OrdinalIgnoreCase))
                        {
                            TranslationFileType = FileType.CSV;
                        }
                        else
                        {
                            // just consider the output as txt format if it doesn't have .csv extension
                            TranslationFileType = FileType.TXT;
                        }
                    }
                }
                else
                {
                    // it is to generate. And Output should point to the directory name.                    
                    if (!Directory.Exists(Output))
                        return StringTable.Get("OutputDirectoryError", Output);
                }
            }

            // Rule #7: if the input assembly path is not null
            if (AssemblyPaths != null && AssemblyPaths.Count > 0)
            {
                Assemblies = new Assembly[AssemblyPaths.Count];
                for (int i = 0; i < Assemblies.Length; i++)
                {
                    try
                    {   // load the assembly                      
                        Assemblies[i] = Assembly.LoadFrom(AssemblyPaths[i]);
                    }
                    catch (Exception ex) when (ex is ArgumentException or FileLoadException or BadImageFormatException
                                                  or FileNotFoundException or PathTooLongException or SecurityException)
                    {   // return error message when loading this assembly
                        return ex.Message;
                    }
                }
            }

            // if we come to this point, we are all fine, return null error message
            return null;
        }

        /// <summary>
        /// Retrieves <see cref="FileType"/> from <paramref name="input"/>, <see langword="null"/> if we didn't recognize it.
        /// </summary>
        private static bool TryGetFileType(ReadOnlySpan<char> input, [NotNullWhen(true)] out FileType? inputType)
        {
            ReadOnlySpan<char> extension = Path.GetExtension(input);
            inputType = null;

            // Get the input file type.
            if (extension.Equals($".{nameof(FileType.BAML)}", StringComparison.OrdinalIgnoreCase))
            {
                inputType = FileType.BAML;
                return true;
            }
            else if (extension.Equals($".{nameof(FileType.RESOURCES)}", StringComparison.OrdinalIgnoreCase))
            {
                inputType = FileType.RESOURCES;
                return true;
            }
            else if (extension.Equals($".{nameof(FileType.DLL)}", StringComparison.OrdinalIgnoreCase))
            {
                inputType = FileType.DLL;
                return true;
            }
            else if (extension.Equals($".{nameof(FileType.EXE)}", StringComparison.OrdinalIgnoreCase))
            {
                inputType = FileType.EXE;
                return true;
            }

            return false;
        }
    }
}
