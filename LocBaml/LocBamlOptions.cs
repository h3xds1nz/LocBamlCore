// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.IO;
using System;

namespace BamlLocalization
{
    /// <summary>
    /// The class that groups all the BAML options together.
    /// </summary>
    internal sealed class LocBamlOptions
    {
        internal string Input;
        internal string Output;
        internal CultureInfo CultureInfo;
        internal string Translations;
        internal bool ToParse;
        internal bool ToGenerate;
        internal bool HasNoLogo;
        internal bool IsVerbose;
        internal FileType TranslationFileType;
        internal FileType InputType;
        internal List<string>? AssemblyPaths;
        internal Assembly[]? Assemblies;

        /// <summary>
        /// return true if the operation succeeded otherwise, return false
        /// </summary>
        internal string? CheckAndSetDefault()
        {
            // we validate the options here and also set default if we can

            // Rule #1: One and only one action at a time
            // i.e. Can't parse and generate at the same time; must do only one of them
            if ((ToParse && ToGenerate) || (!ToParse && !ToGenerate))
                return StringLoader.Get("MustChooseOneAction");

            // Rule #2: Must have an input 
            if (string.IsNullOrEmpty(Input))
            {
                return StringLoader.Get("InputFileRequired");
            }
            else
            {
                if (!File.Exists(Input))
                {
                    return StringLoader.Get("FileNotFound", Input);
                }

                ReadOnlySpan<char> extension = Path.GetExtension(Input.AsSpan());

                // Get the input file type.
                if (extension.Equals($".{nameof(FileType.BAML)}", StringComparison.OrdinalIgnoreCase))
                {
                    InputType = FileType.BAML;
                }
                else if (extension.Equals($".{nameof(FileType.RESOURCES)}", StringComparison.OrdinalIgnoreCase))
                {
                    InputType = FileType.RESOURCES;
                }
                else if (extension.Equals($".{nameof(FileType.DLL)}", StringComparison.OrdinalIgnoreCase))
                {
                    InputType = FileType.DLL;
                }
                else if (extension.Equals($".{nameof(FileType.EXE)}", StringComparison.OrdinalIgnoreCase))
                {
                    InputType = FileType.EXE;
                }
                else
                {
                    return StringLoader.Get("FileTypeNotSupported", extension.ToString());
                }
            }

            if (ToGenerate)
            {
                // Rule #3: before generation, we must have Culture string
                if (CultureInfo == null && InputType != FileType.BAML)
                {
                    // if we are not generating baml, 
                    return StringLoader.Get("CultureNameNeeded", InputType.ToString());
                }

                // Rule #4: before generation, we must have translation file
                if (string.IsNullOrEmpty(Translations))
                {
                    return StringLoader.Get("TranslationNeeded");
                }
                else
                {
                    ReadOnlySpan<char> extension = Path.GetExtension(Translations.AsSpan());

                    if (!File.Exists(Translations))
                    {
                        return StringLoader.Get("TranslationNotFound", Translations);
                    }
                    else
                    {
                        if (extension.Equals($".{nameof(FileType.CSV)}", StringComparison.OrdinalIgnoreCase))
                        {
                            TranslationFileType = FileType.CSV;
                        }
                        else
                        {
                            TranslationFileType = FileType.TXT;
                        }
                    }
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
                    return StringLoader.Get("OutputDirectoryNeeded");
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
                        ReadOnlySpan<char> extension = Path.GetExtension(Output.AsSpan());

                        // ignore case and invariant culture
                        if (extension.Equals($".{nameof(FileType.CSV)}", StringComparison.OrdinalIgnoreCase))
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
                        return StringLoader.Get("OutputDirectoryError", Output);
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
