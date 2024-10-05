// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Resources.Extensions;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// Class that enumerates all the baml streams in the input file for parsing.
    /// </summary>
    internal sealed class BamlStreamList
    {
        private readonly List<BamlStream> _bamlStreams = new();

        /// <summary>
        /// Enumerates all the .baml streams in the <see cref="LocBamlOptions.Input"/> file.
        /// </summary>        
        internal BamlStreamList(LocBamlOptions options)
        {
            switch (options.InputType)
            {
                case FileType.BAML:
                    {
                        _bamlStreams.Add(new BamlStream(Path.GetFileName(options.Input), File.OpenRead(options.Input)));
                        break;
                    }
                case FileType.RESOURCES:
                    {
                        using (DeserializingResourceReader resourceReader = new(options.Input))
                        {
                            // enumerate all bamls in a resources
                            EnumerateBamlInResources(resourceReader, options.Input);
                        }
                        break;
                    }
                case FileType.EXE or FileType.DLL:
                    {
                        // for a dll, it is the same idea
                        Assembly assembly = Assembly.LoadFrom(options.Input);
                        foreach (string resourceName in assembly.GetManifestResourceNames())
                        {
                            ResourceLocation resourceLocation = assembly.GetManifestResourceInfo(resourceName).ResourceLocation;

                            // if this resource is in another assemlby, we will skip it
                            if ((resourceLocation & ResourceLocation.ContainedInAnotherAssembly) != 0)
                            {
                                continue;   // in resource assembly, we don't have resource that is contained in another assembly
                            }

                            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                            using (DeserializingResourceReader reader = new(resourceStream))
                            {
                                EnumerateBamlInResources(reader, resourceName);
                            }
                        }
                        break;
                    }
                default:
                    {

                        Debug.Assert(false, "Not supported type");
                        break;
                    }
            }
        }

        /// <summary>
        /// return the number of baml streams found
        /// </summary>
        internal int Count
        {
            get => _bamlStreams.Count;
        }

        /// <summary>
        /// Gets the baml stream in the input file through indexer
        /// </summary>        
        internal BamlStream this[int i]
        {
            get => _bamlStreams[i];
        }

        /// <summary>
        /// Close the baml streams enumerated
        /// </summary>
        internal void Close()
        {
            for (int i = 0; i < _bamlStreams.Count; i++)
            {
                _bamlStreams[i].Close();
            }
        }

        /// <summary>
        /// Enumerate baml streams in a resources file
        /// </summary>        
        private void EnumerateBamlInResources(DeserializingResourceReader reader, string resourceName)
        {
            foreach (DictionaryEntry entry in reader)
            {
                string name = entry.Key as string;
                if (BamlStream.IsResourceEntryBamlStream(name, entry.Value))
                {
                    _bamlStreams.Add(new BamlStream(BamlStream.CombineBamlStreamName(resourceName, name), (Stream)entry.Value));
                }
            }
        }
    }
}
