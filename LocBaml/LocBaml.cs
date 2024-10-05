// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using BamlLocalization.ConsoleSupport;
using System.Collections.Generic;
using BamlLocalization.Resources;
using BamlLocalization.Options;
using System.Globalization;
using System.Reflection;
using System.IO;
using System;

namespace BamlLocalization
{
    /// <summary>
    /// LocBaml tool: A command line tool to localize baml
    /// </summary>
    public static class LocBaml
    {
        // Private constants
        private const int ErrorCode = 100;        
        private const int SuccessCode = 0;

        // Supported command line options
        // NOTE: "*" prefix means the option must have a value; options without "*" means the option can't have a value 
        private static readonly string[] s_supportedArguments = ["parse",        // /parse           for update
                                                                 "generate",     // /generate        for generate
                                                                 "*out",         // /out             for output .csv|.txt when parsing, for output directory when generating
                                                                 "*culture",     // /culture         for culture name
                                                                 "*translation", // /translation     for translation file, .csv|.txt
                                                                 "*asmpath",     // /asmpath         for assembly path to look for references
                                                                 "nologo",       // /nologo          for not to print logo      
                                                                 "help",         // /help            for help
                                                                 "verbose"];     // /verbose         for verbose output

        [STAThread]
        public static int Main(string[] args)
        {

            GetCommandLineOptions(args, out LocBamlOptions? options, out string? errorMessage);

            if (errorMessage != null)
            {
                // there are errors                
                PrintLogo(options);
                Console.WriteLine(StringLoader.Get("ErrorMessage", errorMessage));                
                Console.WriteLine();
                PrintUsage();
                return ErrorCode;    // error
            }          

             // at this point, we obtain good options.
            if (options == null)            
            {
                // no option to process. Noop.
                return SuccessCode;
            }

            PrintLogo(options);

            try
            {
                // We can either parse or generate resources
                if (options is ParseOptions parseOptions)
                {
                    ParseBamlResources(parseOptions);
                }
                else if (options is GenerateOptions generateOptions)
                {
                    GenerateBamlResources(generateOptions);
                }
                else
                    throw new InvalidOperationException("Unknown LocBamlOptions type");
            }
            catch (Exception)                
            {
#if DEBUG
                throw;
#else
                Console.WriteLine(e.Message);
                return ErrorCode;            
#endif
            }

            return SuccessCode;
        
        }        

        /// <summary>
        /// Parse the baml resources given in the command line
        /// </summary>        
        private static void ParseBamlResources(ParseOptions options)
        {            
            TranslationDictionariesWriter.Write(options);         
        }

        /// <summary>
        /// Genereate localized baml 
        /// </summary>        
        private static void GenerateBamlResources(GenerateOptions options)
        {   
            Stream input = File.OpenRead(options.Translations);
            using (ResourceTextReader reader = new(options.TranslationsSourceType, input))
            {   
                TranslationDictionariesReader dictionaries = new(reader);                                                               
                ResourceGenerator.Generate(options, dictionaries);
            }         
        }
            
        /// <summary>
        /// get CommandLineOptions, return error message
        /// </summary>
        private static void GetCommandLineOptions(string[] args, out LocBamlOptions? options, out string? errorMessage)
        {
            // This will only be set when we succeeded
            options = null;

            CommandLineParser commandLineParser; 
            try
            {
                commandLineParser = new CommandLineParser(args, s_supportedArguments);
            }            
            catch (ArgumentException e)
            {
                errorMessage = e.Message;
                return;
            }

            if (commandLineParser.ArgumentsCount + commandLineParser.OptionsCount < 1)
            {
                PrintLogo(null);
                PrintUsage();
                errorMessage = null;
                return;
            }

            LocBamlCreationOptions creationOptions  = new() { Input = commandLineParser.GetNextArg() };
            while (commandLineParser.GetNextOption() is CommandLineOption commandLineOption)
            {
                if (commandLineOption.Name      == "parse")
                {
                    creationOptions.ToParse = true;
                }
                else if (commandLineOption.Name == "generate")
                {
                    creationOptions.ToGenerate = true;
                }
                else if (commandLineOption.Name == "nologo")
                {
                    creationOptions.HasNoLogo = true;                        
                }
                else if (commandLineOption.Name == "help")
                {
                    // we print usage and stop processing
                    PrintUsage();
                    errorMessage = null;
                    return;
                }
                else if (commandLineOption.Name == "verbose")
                {
                    creationOptions.IsVerbose = true;
                }
                    // the following ones need value
                else if (commandLineOption.Name == "out")
                {
                    creationOptions.Output = commandLineOption.Value;
                }
                else if (commandLineOption.Name == "translation")
                {
                    creationOptions.Translations = commandLineOption.Value;
                }
                else if (commandLineOption.Name == "asmpath")
                {
                    // Lazy initialization, create if NULL
                    creationOptions.AssemblyPaths ??= new List<string>();

                    creationOptions.AssemblyPaths.Add(commandLineOption.Value);
                }
                else if (commandLineOption.Name == "culture")
                {
                    try
                    {
                        creationOptions.CultureInfo = new CultureInfo(commandLineOption.Value);
                    }
                    catch (ArgumentException e) // And that's how we find out the culture was not valid
                    {
                        errorMessage = e.Message;
                        options = null;
                        return;
                    }
                }
                else
                {
                    // something that we don't recognize
                    errorMessage = StringLoader.Get("Err_InvalidOption", commandLineOption.Name);
                    options = null;
                    return;
                }
            }

            // we passed all the test till here. Now check the combinations of the options
            errorMessage = creationOptions.VerifyOptions();

            // Create options based on whether we're generating or parsing
            if (errorMessage is null)
                options = creationOptions.ToGenerate ? new GenerateOptions(creationOptions) : new ParseOptions(creationOptions);
        }

        private static void PrintLogo(LocBamlOptions? option)
        {
            if (!option?.HasNoLogo ?? false)
            {               
                Console.WriteLine(StringLoader.Get("Msg_Copyright", GetAssemblyVersion()));
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(StringLoader.Get("Msg_Usage"));
        }         

        private static string GetAssemblyVersion()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();                                   
            return currentAssembly.GetName().Version.ToString(4);
        }
    }
}