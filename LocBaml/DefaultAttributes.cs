// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Collections.Generic;
using System.Windows;
using System;

namespace BamlLocalization
{
    /// <summary>
    /// Defines all the static localizability attributes
    /// </summary>
    internal static class DefaultAttributes
    {
        /// <summary>
        /// Stores pre-defined attribute for CLR types
        /// </summary>
        private static readonly Dictionary<object, LocalizabilityAttribute> DefinedAttributes;  

        static DefaultAttributes()
        {
            // predefined localizability attributes
            DefinedAttributes = new Dictionary<object, LocalizabilityAttribute>(16);

            // nonlocalizable attributes
            LocalizabilityAttribute notReadable = new(LocalizationCategory.None) { Readability = Readability.Unreadable };
            LocalizabilityAttribute notModifiable = new(LocalizationCategory.None) { Modifiability = Modifiability.Unmodifiable };

            // not localizable CLR types
            DefinedAttributes.Add(typeof(Boolean),   notReadable);
            DefinedAttributes.Add(typeof(Byte),      notReadable);
            DefinedAttributes.Add(typeof(SByte),     notReadable);
            DefinedAttributes.Add(typeof(Char),      notReadable);
            DefinedAttributes.Add(typeof(Decimal),   notReadable);
            DefinedAttributes.Add(typeof(Double),    notReadable);            
            DefinedAttributes.Add(typeof(Single),    notReadable);            
            DefinedAttributes.Add(typeof(Int32),     notReadable);            
            DefinedAttributes.Add(typeof(UInt32),    notReadable);            
            DefinedAttributes.Add(typeof(Int64),     notReadable);
            DefinedAttributes.Add(typeof(UInt64),    notReadable);            
            DefinedAttributes.Add(typeof(Int16),     notReadable);            
            DefinedAttributes.Add(typeof(UInt16),    notReadable);    
            DefinedAttributes.Add(typeof(Uri),       notModifiable);
        }   
        
        /// <summary>
        /// Get the localizability attribute for a type
        /// </summary>
        internal static LocalizabilityAttribute GetDefaultAttribute(object type)
        {
            if (DefinedAttributes.TryGetValue(type, out LocalizabilityAttribute predefinedAttribute))
            {
                // create a copy of the predefined attribute and return the copy
                return new LocalizabilityAttribute(predefinedAttribute.Category)
                {
                    Readability = predefinedAttribute.Readability,
                    Modifiability = predefinedAttribute.Modifiability
                };
            }
       
            if (type is Type targetType && targetType.IsValueType)
            {
                // It is looking for the default value of a value type (i.e. struct and enum) we use this default.
                return new LocalizabilityAttribute(LocalizationCategory.Inherit)
                {
                    Modifiability = Modifiability.Unmodifiable
                };             
            }

            // Default Fallback attribute
            return new LocalizabilityAttribute(LocalizationCategory.Inherit);
        } 
    }
}
