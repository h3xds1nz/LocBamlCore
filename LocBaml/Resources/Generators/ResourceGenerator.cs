// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Markup.Localizer;
using System.Reflection.Metadata;
using BamlLocalization.Options;
using BamlLocalization.Data;
using System.Globalization;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.IO;
using System;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// ResourceGenerator class, it generates the localized baml from translations.
    /// </summary>
    internal static class ResourceGenerator
    {
        /// <summary>
        /// Generates localized Baml from translations
        /// </summary>
        /// <param name="options">LocBaml options</param>
        /// <param name="dictionaries">the translation dictionaries</param>
        internal static void Generate(GenerateOptions options, TranslationDictionariesReader dictionaries)
        {
            // base on the input, we generate differently            
            switch (options.InputType)
            {
                case FileType.BAML:
                    {
                        // input file name
                        string bamlName = Path.GetFileName(options.Input);

                        // outpuf file name is Output dir + input file name
                        string outputFileName = GetOutputFileName(options);

                        // construct the full path
                        string fullPathOutput = Path.Combine(options.Output, outputFileName);

                        options.Write(StringTable.Get("GenerateBaml", fullPathOutput));

                        using (Stream input = File.OpenRead(options.Input))
                        {
                            using (Stream output = new FileStream(fullPathOutput, FileMode.Create))
                            {
                                BamlLocalizationDictionary? dictionary = dictionaries[bamlName];

                                // if it is null, just create an empty dictionary.
                                dictionary ??= new BamlLocalizationDictionary();
                                GenerateBamlStream(input, output, dictionary, options);
                            }
                        }

                        options.WriteLine(StringTable.Get("Done"));
                        break;
                    }
                case FileType.RESOURCES:
                    {
                        string outputFileName = GetOutputFileName(options);
                        string fullPathOutput = Path.Combine(options.Output, outputFileName);

                        using (Stream input = File.OpenRead(options.Input))
                        {
                            using (Stream output = File.OpenWrite(fullPathOutput))
                            {
                                // create a Resource reader on the input;
                                ResourceReader reader = new(input);

                                // create a writer on the output;
                                ResourceWriter writer = new(output);

                                GenerateResourceStream(
                                    options,         // options
                                    options.Input,   // resources name
                                    reader,          // resource reader
                                    writer,          // resource writer
                                    dictionaries);   // translations

                                reader.Close();

                                // now generate and close
                                writer.Generate();
                                writer.Close();
                            }
                        }

                        options.WriteLine(StringTable.Get("DoneGeneratingResource", outputFileName));
                        break;
                    }
                case FileType.EXE:
                case FileType.DLL:
                    {
                        GenerateAssembly(options, dictionaries);
                        break;
                    }
                default:
                    {
                        Debug.Assert(false, "Can't generate to this type");
                        break;
                    }
            }
        }


        private static void GenerateBamlStream(Stream input, Stream output, BamlLocalizationDictionary dictionary, LocBamlOptions options)
        {
            string commentFile = Path.ChangeExtension(options.Input, "loc");
            TextReader? commentStream = null;

            try
            {
                if (File.Exists(commentFile))
                    commentStream = new StreamReader(commentFile);

                // create a localizabilty resolver based on reflection
                BamlLocalizabilityByReflection localizabilityReflector = new(options.Assemblies);

                // create baml localizer
                BamlLocalizer mgr = new(input, localizabilityReflector, commentStream);

                // get the resources
                BamlLocalizationDictionary source = mgr.ExtractResources();
                BamlLocalizationDictionary translations = new BamlLocalizationDictionary();

                foreach (DictionaryEntry entry in dictionary)
                {
                    BamlLocalizableResourceKey key = (BamlLocalizableResourceKey)entry.Key;
                    // filter out unchanged items
                    if (!source.Contains(key) || entry.Value == null || source[key].Content != ((BamlLocalizableResource)entry.Value).Content)
                    {
                        translations.Add(key, (BamlLocalizableResource)entry.Value);
                    }
                }

                // update baml
                mgr.UpdateBaml(output, translations);
            }
            finally
            {
                commentStream?.Close();
            }
        }

        private static void GenerateResourceStream(
                LocBamlOptions options,                     // options from the command line
                string resourceName,                        // the name of the .resources file
                ResourceReader reader,                      // the reader for the .resources
                ResourceWriter writer,                      // the writer for the output .resources
                TranslationDictionariesReader dictionaries  // the translations
            )
        {
            // Indent the logger
            options.Write(Environment.NewLine);

            // enumerate through each resource and generate it
            foreach (DictionaryEntry entry in reader)
            {
                string name = entry.Key as string;
                object? resourceValue = null;

                // See if it looks like a Baml resource
                if (BamlStream.IsResourceEntryBamlStream(name, entry.Value))
                {
                    Stream? targetStream = null;
                    options.Write("    ");
                    options.Write(StringTable.Get("GenerateBaml", name));

                    // grab the localizations available for this Baml
                    string bamlName = BamlStream.CombineBamlStreamName(resourceName, name);
                    BamlLocalizationDictionary? localizations = dictionaries[bamlName];
                    if (localizations != null)
                    {
                        targetStream = new MemoryStream();

                        // generate into a new Baml stream
                        GenerateBamlStream(
                            (Stream)entry.Value,
                            targetStream,
                            localizations,
                            options
                        );
                    }
                    options.WriteLine(StringTable.Get("Done"));

                    // sets the generated object to be the generated baml stream
                    resourceValue = targetStream;
                }

                if (resourceValue == null)
                {
                    //
                    // The stream is not localized as Baml yet, so we will make a copy of this item into 
                    // the localized resources
                    //

                    // We will add the value as is if it is serializable. Otherwise, make a copy
                    resourceValue = entry.Value;

                    object[] serializableAttributes = resourceValue.GetType().GetCustomAttributes(typeof(SerializableAttribute), true);
                    if (serializableAttributes.Length == 0)
                    {
                        // The item returned from resource reader is not serializable
                        // If it is Stream, we can wrap all the values in a MemoryStream and 
                        // add to the resource. Otherwise, we had to skip this resource.
                        if (resourceValue is Stream resourceStream)
                        {
                            byte[] buffer = new byte[resourceStream.Length];
                            MemoryStream targetStream = new(buffer);
                            resourceStream.ReadExactly(buffer);

                            resourceValue = targetStream;
                        }
                    }
                }

                if (resourceValue != null)
                {
                    writer.AddResource(name, resourceValue);
                }
            }
        }

        private static void GenerateStandaloneResource(string fullPathName, Stream resourceStream)
        {
            // simply do a copy for the stream
            using (FileStream file = new(fullPathName, FileMode.Create, FileAccess.Write))
            {
                const int BUFFER_SIZE = 4096;
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead = 1;
                while (bytesRead > 0)
                {
                    bytesRead = resourceStream.Read(buffer, 0, BUFFER_SIZE);
                    file.Write(buffer, 0, bytesRead);
                }
            }
        }

        //--------------------------------------------------
        // The function follows Managed code parser
        // implementation. in the future, maybe they should 
        // share the same code
        //--------------------------------------------------
        private static void GenerateAssembly(GenerateOptions options, TranslationDictionariesReader dictionaries)
        {
            // source assembly full path 
            string sourceAssemblyFullName = options.Input;
            // output assembly directory
            string outputAssemblyDir = options.Output;
            // output assembly name
            string outputAssemblyLocalName = GetOutputFileName(options);
            // the module name within the assembly
            string moduleLocalName = GetAssemblyModuleLocalName(options, outputAssemblyLocalName);

            // get the source assembly
            Assembly srcAsm = Assembly.LoadFrom(sourceAssemblyFullName);

            // obtain the assembly name
            AssemblyName targetAssemblyNameObj = srcAsm.GetName();

            // store the culture info of the source assembly
            CultureInfo srcCultureInfo = targetAssemblyNameObj.CultureInfo;

            // update it to use it for target assembly
            targetAssemblyNameObj.Name = Path.GetFileNameWithoutExtension(outputAssemblyLocalName);
            targetAssemblyNameObj.CultureInfo = options.TargetCulture;

            // we get an assembly builder
            MetadataBuilder metadataBuilder = new();
            metadataBuilder.AddAssembly(metadataBuilder.GetOrAddString(targetAssemblyNameObj.Name), targetAssemblyNameObj.Version,
                                        metadataBuilder.GetOrAddString(targetAssemblyNameObj.CultureName), default, 0, AssemblyHashAlgorithm.None);

            // we create a module builder for embedded resource modules
            metadataBuilder.AddModule(0, metadataBuilder.GetOrAddString(moduleLocalName), metadataBuilder.GetOrAddGuid(new Guid()), default, default);

            // Create a resource blob where we will store all our resources
            BlobBuilder resourceBlob = new BlobBuilder(4096);

            options.WriteLine(StringTable.Get("GenerateAssembly", targetAssemblyNameObj.FullName));

            // now for each resource in the assembly
            foreach (string resourceName in srcAsm.GetManifestResourceNames())
            {
                // get the resource location for the resource
                // NOTE: Suppressed because resourceName is valid
                ResourceLocation resourceLocation = srcAsm.GetManifestResourceInfo(resourceName)!.ResourceLocation;

                // if this resource is in another assembly, we will skip it
                if ((resourceLocation & ResourceLocation.ContainedInAnotherAssembly) != 0)
                    continue;   // in resource assembly, we don't have resource that is contained in another assembly

                // gets the neutral resource name, giving it the source culture info
                string neutralResourceName = GetNeutralResModuleName(resourceName, srcCultureInfo);

                // gets the target resource name, by giving it the target culture info
                string targetResourceName = GetCultureSpecificResourceName(neutralResourceName, options.TargetCulture);

                // resource stream
                // NOTE: Suppressed because resourceName is valid
                Stream resourceStream = srcAsm.GetManifestResourceStream(resourceName)!;

                // see if it is a .resources
                if (neutralResourceName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    // now we think we have resource stream 
                    // get the resource writer
                    MemoryStream stream = new MemoryStream();
                    ResourceWriter writer = new ResourceWriter(stream);
                    // check if it is a embeded assembly
                    // TODO: Figure out whether we need this check in .NET Core and what should we do differently
                    if ((resourceLocation & ResourceLocation.Embedded) != 0)
                    {
                        // Define resource ahead of time, this will spare us from having to calculate the offset
                        // OLD COMMENT: gets the resource writer from the module builder
                        metadataBuilder.AddManifestResource(ManifestResourceAttributes.Public, metadataBuilder.GetOrAddString(targetResourceName), default, (uint)resourceBlob.Count);
                    }
                    else
                    {
                        // OLD COMMENT: it is a standalone resource, we get the resource writer from the assembly builder
                        metadataBuilder.AddManifestResource(ManifestResourceAttributes.Public, metadataBuilder.GetOrAddString(targetResourceName), default, (uint)resourceBlob.Count);
                    }

                    // get the resource reader
                    ResourceReader reader = new(resourceStream);

                    // generate the resources
                    options.WriteLine(StringTable.Get("GenerateResource", targetResourceName));
                    GenerateResourceStream(options, resourceName, reader, writer, dictionaries);

                    // make sure this has been flushed
                    writer.Generate();

                    // Length is 4 bytes long, int only (2GB)
                    Debug.Assert(stream.Length <= int.MaxValue);

                    // You always prepend with length due to the alignment
                    resourceBlob.WriteInt32((int)stream.Length);
                    resourceBlob.WriteBytes(stream.ToArray());

                    // That's what roslyn does
                    resourceBlob.Align(8);
                    writer.Close();
                }
                else // TODO: This whole thing should just write into resourceBlob since there's no AddResourceFile in .NET
                {
                    // else it is a stand alone untyped manifest resources.
                    ReadOnlySpan<char> extension = Path.GetExtension(targetResourceName.AsSpan());

                    string fullFileName = Path.Combine(outputAssemblyDir, targetResourceName);

                    // check if it is a .baml, case-insensitive
                    if (extension.Equals(".baml", StringComparison.OrdinalIgnoreCase))
                    {
                        // try to localized the the baml
                        // find the resource dictionary
                        BamlLocalizationDictionary? dictionary = dictionaries[resourceName];

                        // if it is null, just create an empty dictionary.
                        if (dictionary is not null)
                        {
                            // it is a baml stream
                            using (Stream output = File.OpenWrite(fullFileName))
                            {
                                options.Write("    ");
                                options.WriteLine(StringTable.Get("GenerateStandaloneBaml", fullFileName));
                                GenerateBamlStream(resourceStream, output, dictionary, options);
                                options.WriteLine(StringTable.Get("Done"));
                            }
                        }
                        else
                        {
                            // can't find localization of it, just copy it
                            GenerateStandaloneResource(fullFileName, resourceStream);
                        }
                    }
                    else
                    {
                        // it is an untyped resource stream, just copy it
                        GenerateStandaloneResource(fullFileName, resourceStream);
                    }

                    // now add this resource file into the assembly
                    // NOTE: See TODO at the top of this statement
                    //targetAssemblyBuilder.AddResourceFile(
                    //    targetResourceName,           // resource name
                    //    targetResourceName,           // file name
                    //    ResourceAttributes.Public     // visibility of the resource to other assembly
                    //);

                }
            }

            // at the end, generate the assembly

            PEHeaderBuilder peHeaderBuilder = new(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll, subsystem: Subsystem.WindowsGui);
            ManagedPEBuilder peBuilder = new(peHeaderBuilder, new MetadataRootBuilder(metadataBuilder), new BlobBuilder(), managedResources: resourceBlob);

            BlobBuilder blob = new();
            peBuilder.Serialize(blob);

            using (FileStream fileStream = new(Path.Join(outputAssemblyDir, outputAssemblyLocalName), FileMode.Create, FileAccess.Write))
                blob.WriteContentTo(fileStream);

            options.WriteLine(StringTable.Get("DoneGeneratingAssembly"));
        }


        //-----------------------------------------
        // private function dealing with naming 
        //-----------------------------------------

        // return the local output file name, i.e. without directory
        private static string GetOutputFileName(GenerateOptions options)
        {
            string inputFileName = Path.GetFileName(options.Input);

            switch (options.InputType)
            {
                case FileType.BAML:
                    return inputFileName;
                case FileType.EXE:
                    return inputFileName.Remove(inputFileName.LastIndexOf('.')) + ".resources.dll";
                case FileType.DLL:
                    return inputFileName;
                case FileType.RESOURCES:
                    {
                        // get the output file name
                        ReadOnlySpan<char> outputFileName = inputFileName;

                        // get to the last dot seperating filename and extension
                        int lastDot = outputFileName.LastIndexOf('.');
                        int secondLastDot = outputFileName.Slice(lastDot - 1).LastIndexOf('.');
                        if (secondLastDot > 0)
                        {
                            string cultureName = inputFileName.Substring(secondLastDot + 1, lastDot - secondLastDot - 1);
                            if (LocBamlConst.IsValidCultureName(cultureName))
                            {
                                ReadOnlySpan<char> extension = outputFileName.Slice(lastDot);
                                ReadOnlySpan<char> frontPart = outputFileName.Slice(0, secondLastDot + 1);

                                return $"{frontPart}{options.TargetCulture.Name}{extension}";
                            }
                        }
                        return inputFileName;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private static string GetAssemblyModuleLocalName(GenerateOptions options, string targetAssemblyName)
        {
            ReadOnlySpan<char> finalAssemblyName = targetAssemblyName.AsSpan();
            if (finalAssemblyName.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            {
                // we create the satellite assembly name
                return $"{finalAssemblyName.Slice(0, finalAssemblyName.Length - ".resources.dll".Length)}.{options.TargetCulture.Name}.resources.dll";
            }

            return targetAssemblyName;
        }

        // return the neutral resource name
        private static string GetNeutralResModuleName(string resourceName, CultureInfo cultureInfo)
        {
            if (cultureInfo.Equals(CultureInfo.InvariantCulture))
                return resourceName;

            // if it is an satellite assembly, we need to strip out the culture name
            string normalizedName = resourceName.ToLower(CultureInfo.InvariantCulture);
            int end = normalizedName.LastIndexOf(".resources");

            if (end == -1)
                return resourceName;

            int start = normalizedName.LastIndexOf('.', end - 1);

            if (start > 0 && end - start > 0)
            {
                ReadOnlySpan<char> cultureTag = resourceName.AsSpan().Slice(start + 1, end - start - 1);

                if (cultureTag.Equals(cultureInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // it has the correct culture name, so we can take it out
                    return resourceName.Remove(start, end - start);
                }
            }
            return resourceName;
        }

        private static string GetCultureSpecificResourceName(string neutralResourceName, CultureInfo culture)
        {
            // gets the extension
            ReadOnlySpan<char> extension = Path.GetExtension(neutralResourceName.AsSpan());

            // swap in culture name
            string cultureName = Path.ChangeExtension(neutralResourceName, culture.Name);

            // return the new name with the same extension
            return string.Concat(cultureName, extension);
        }
    }
}
