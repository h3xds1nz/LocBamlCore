// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 6th Oct 2024
// by h3xds1nz

using System.Windows.Markup.Localizer;
using System.Collections.Generic;
using System.Windows;
using System;

namespace BamlLocalization.Resources
{
    /// <summary>
    /// Reader to read the translations from CSV or tab-separated txt file    
    /// </summary> 
    internal sealed class TranslationDictionariesReader
    {
        // hashtable that maps from baml name to its ResourceDictionary (case-insensitive)
        private readonly Dictionary<string, BamlLocalizationDictionary> _table = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">resoure text reader that reads CSV or a tab-separated txt file</param>
        internal TranslationDictionariesReader(ResourceTextReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            // we read each Row
            int rowNumber = 0;
            while (reader.ReadRow())
            {
                rowNumber++;

                // field #1 is the baml name.
                string? bamlName = reader.GetColumn(0);

                // it can't be null
                if (bamlName == null)
                    throw new ApplicationException(StringLoader.Get("EmptyRowEncountered"));

                if (string.IsNullOrEmpty(bamlName))
                {
                    // allow for comment lines in csv file.
                    // each comment line starts with ",". It will make the first entry as String.Empty.
                    // and we will skip the whole line.
                    continue;   // if the first column is empty, take it as a comment line
                }

                // field #2: key to the localizable resource
                string? key = reader.GetColumn(1);
                if (key == null)
                    throw new ApplicationException(StringLoader.Get("NullBamlKeyNameInRow"));

                BamlLocalizableResourceKey resourceKey = LocBamlConst.StringToResourceKey(key);

                // get the dictionary               
                if (!_table.TryGetValue(bamlName, out BamlLocalizationDictionary? dictionary))
                {
                    // we create one if it is not there yet.
                    dictionary = new BamlLocalizationDictionary();
                    _table[bamlName] = dictionary;
                }

                BamlLocalizableResource? resource;

                // the rest of the fields are either all null,
                // or all non-null. If all null, it means the resource entry is deleted.

                // get the string category
                string? categoryString = reader.GetColumn(2);
                if (categoryString == null)
                {
                    // it means all the following fields are null starting from column #3.
                    resource = null;
                }
                else
                {
                    // the rest must all be non-null.
                    // the last cell can be null if there is no content
                    for (int i = 3; i < 6; i++)
                    {
                        if (reader.GetColumn(i) == null)
                            throw new Exception(StringLoader.Get("InvalidRow"));
                    }

                    // now we know all are non-null. let's try to create a resource
                    resource = new BamlLocalizableResource();

                    // field #3: Category
                    resource.Category = Enum.Parse<LocalizationCategory>(categoryString);

                    // field #4: Readable
                    resource.Readable = bool.Parse(reader.GetColumn(3).AsSpan().Trim());

                    // field #5: Modifiable
                    resource.Modifiable = bool.Parse(reader.GetColumn(4).AsSpan().Trim());

                    // field #6: Comments
                    resource.Comments = reader.GetColumn(5);

                    // field #7: Content
                    resource.Content = reader.GetColumn(6);

                    // in case content being the last column, consider null as empty.
                    resource.Content ??= string.Empty;

                    // field > #7: Ignored.
                }

                // at this point, we are good.
                // add to the dictionary.
                dictionary.Add(resourceKey, resource);
            }
        }

        // TODO: This should be changed, preferrably just introduce TryGetValue
        internal BamlLocalizationDictionary? this[string key]
        {
            get => _table.TryGetValue(key, out BamlLocalizationDictionary? dictionary) ? dictionary : null;
            set => _table[key] = value;
        }
    }
}
