// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Windows.Markup.Localizer;
using BamlLocalization.Options;
using BamlLocalization.Data;
using System.Collections;
using System.Reflection;
using System.IO;
using System;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// Writer to write out localizable values into CSV or tab-separated txt files.     
    /// </summary>
    internal static class TranslationDictionariesWriter
    {
        /// <summary>
        /// Write the localizable key-value pairs
        /// </summary>
        /// <param name="options"></param>
        internal static void Write(ParseOptions options)
        {
            options.WriteLine(StringTable.Get("CreateTranslationsFile", options.Output));
            Stream output = new FileStream(options.Output, FileMode.Create);

            BamlStreamList bamlStreamList = new(options);
            using (ResourceTextWriter writer = new(options.TranslationsTargetType, output))
            {
                options.WriteLine(StringTable.Get("WriteBamlValues"));
                // Indent the logger
                options.Write(Environment.NewLine);
                for (int i = 0; i < bamlStreamList.Count; i++)
                {
                    options.Write("    ");
                    options.Write(StringTable.Get("ProcessingBaml", bamlStreamList[i].Name));

                    // Search for comment file in the same directory. The comment file has the extension to be "loc".
                    string commentFile = Path.ChangeExtension(bamlStreamList[i].Name, "loc");
                    TextReader? commentStream = null;

                    try
                    {
                        if (File.Exists(commentFile))
                        {
                            commentStream = new StreamReader(commentFile);
                        }

                        // create the baml localizer
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                        BamlLocalizer mgr = new(bamlStreamList[i].Stream, new BamlLocalizabilityByReflection(options.Assemblies), commentStream);
                        AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

                        // extract localizable resource from the baml stream
                        BamlLocalizationDictionary dict = mgr.ExtractResources();

                        // write out each resource
                        foreach (DictionaryEntry entry in dict)
                        {
                            // column 1: baml stream name
                            writer.WriteColumn(bamlStreamList[i].Name);

                            BamlLocalizableResourceKey key = (BamlLocalizableResourceKey)entry.Key;
                            // TODO: I can't remember from two months ago whether the value can be NULL, so watch out for this suppression
                            BamlLocalizableResource resource = (BamlLocalizableResource)entry.Value!;

                            // column 2: localizable resource key
                            writer.WriteColumn(LocBamlConst.ResourceKeyToString(key));

                            // column 3: localizable resource's category
                            writer.WriteColumn(resource.Category.ToString());

                            // column 4: localizable resource's readability
                            writer.WriteColumn(resource.Readable.ToString());

                            // column 5: localizable resource's modifiability
                            writer.WriteColumn(resource.Modifiable.ToString());

                            // column 6: localizable resource's localization comments
                            writer.WriteColumn(resource.Comments);

                            // column 7: localizable resource's content
                            writer.WriteColumn(resource.Content);

                            // Done. finishing the line
                            writer.EndLine();
                        }

                        options.WriteLine(StringTable.Get("Done"));
                    }
                    finally
                    {
                        commentStream?.Close();
                    }
                }

                // close all the baml input streams, output stream is closed by writer.
                bamlStreamList.Close();
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            // TODO: We might wanna give the users an ability to provide a custom assembly in case they've forgotten
            return null;
        }
    }
}
