// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Windows.Markup.Localizer;
using BamlLocalization.Data;
using System.Globalization;
using System.Diagnostics;
using System;

namespace BamlLocalization
{
    internal static class LocBamlConst
    {
        internal const string BamlAndResourceSeperator = ":";
        internal const string ResourceExtension        = ".resources";

        internal static char GetDelimiter(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.CSV:
                     return ',';

                case FileType.TXT:
                    return '\t';

                default:
                {
                    Debug.Assert(false, "Unsupported FileType");
                    return ','; 
                }
            }
        }

        internal static bool IsValidCultureName(string name)
        {
            try 
            {
                // try create a culture to see if the name is a valid culture name
                CultureInfo culture = new(name);
                return culture != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static string ResourceKeyToString(BamlLocalizableResourceKey key)
        {
            return $"{key.Uid}:{key.ClassName}.{key.PropertyName}";
        }

        internal static BamlLocalizableResourceKey StringToResourceKey(string value)
        {
            int nameEnd = value.LastIndexOf(':');
            if (nameEnd < 0)
            {
                throw new ArgumentException(StringTable.Get("ResourceKeyFormatError"));
            }

            string name  = value.Substring(0, nameEnd);
            int classEnd = value.LastIndexOf('.');
            
            if (classEnd < 0 || classEnd < nameEnd || classEnd == value.Length)
            {
                throw new ArgumentException(StringTable.Get("ResourceKeyFormatError"));
            }

            string className = value.Substring(nameEnd + 1, classEnd - nameEnd - 1);
            string propertyName = value.Substring(classEnd + 1);

            return new BamlLocalizableResourceKey(name, className, propertyName);            
        }
    }    
}
