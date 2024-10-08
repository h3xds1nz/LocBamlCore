// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Modified 5th Oct 2024
// by h3xds1nz

using System.Windows.Markup.Localizer;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System;

namespace BamlLocalization
{
    /// <summary>
    /// BamlLocalizabilityResolver class, it resolves localizabilities of a class/element.
    /// </summary>
    public sealed class BamlLocalizabilityByReflection : BamlLocalizabilityResolver
    {      
        /// Take in an optional list of assemblies in addition to the 
        /// default well-known avalon assemblies. The assemblies will be searched first
        /// in order before the well-known avalon assemblies.
        /// </summary>
        /// <param name="assemblies">additinal list of assemblies to search for Type information</param>
        public BamlLocalizabilityByReflection(params ReadOnlySpan<Assembly> assemblies)
        {
            if (!assemblies.IsEmpty)
            {
                // create the table
                _assemblies = new Dictionary<string, Assembly>(assemblies.Length);
     
                for (int i = 0; i < assemblies.Length; i++)
                {
                    // skip the null ones. 
                    if (assemblies[i] != null)
                    {
                        // index it by the name;
                        _assemblies[assemblies[i].FullName] = assemblies[i];
                    }
                }
            }           

            // create the cache for Type here
            _typeCache = new Dictionary<string, Type?>(32);
        }        

        /// <summary>
        /// Return the localizability of an element to the BamlLocalizer
        /// </summary>
        public override ElementLocalizability GetElementLocalizability(string assembly, string className)
        {
            ElementLocalizability loc = new();

            Type? type = GetType(assembly, className);            
            if (type != null)
            {
                // We found the type, now try to get the localizability attribte from the type
                loc.Attribute = GetLocalizabilityFromType(type);
            }            
            
            // fill in the formatting tag
            int index = Array.IndexOf(FormattedElements, className);
            if (index >= 0)
            {
                loc.FormattingTag = FormattingTag[index];
            }

            return loc;
        }

        /// <summary>
        /// return localizability of a property to the BamlLocalizer
        /// </summary>
        public override LocalizabilityAttribute? GetPropertyLocalizability(string assembly, string className, string property)
        {
            LocalizabilityAttribute? attribute = null;

            Type? type = GetType(assembly, className);        
            if (type != null)
            {
                // type of the property. The type can be retrieved from CLR property, or Attached property.
                Type? attachedPropertyType = null;

                // we found the type. try to get to the property as Clr property                    
                GetLocalizabilityForClrProperty(property, type, out attribute, out Type? clrPropertyType);

                if (attribute == null)
                {
                    // we didn't find localizability as a Clr property on the type,
                    // try to get the property as attached property
                    GetLocalizabilityForAttachedProperty(property, type, out attribute, out attachedPropertyType);
                }

                // if attached property doesn't have [LocalizabilityAttribute] defined,
                // we get it from the type of the property.
                attribute ??= clrPropertyType is not null ? GetLocalizabilityFromType(clrPropertyType) : GetLocalizabilityFromType(attachedPropertyType);
            }

            return attribute;
        }

        /// <summary>
        /// Resolve a formatting tag back to the actual class name
        /// </summary>
        public override string? ResolveFormattingTagToClass(string formattingTag)
        {
            int index = Array.IndexOf(FormattingTag, formattingTag);
            if (index >= 0)
                return FormattedElements[index];
            else
                return null;   
        }

        /// <summary>
        /// Resolve a class name back to its containing assembly 
        /// </summary>
        public override string? ResolveAssemblyFromClass(string className)
        {
            // search through the well-known assemblies
            for (int i = 0; i < _wellKnownAssemblies.Length; i++)
            {
                if (_wellKnownAssemblies[i] is null)
                {
                    _wellKnownAssemblies[i] = Assembly.Load(GetCompatibleAssemblyName(_wellKnownAssemblyNames[i]));
                }

                if (_wellKnownAssemblies[i] is not null && _wellKnownAssemblies[i].GetType(className) is not null)
                {
                    return _wellKnownAssemblies[i].GetName().FullName;
                }
            }

            // search through the custom assemblies
            if (_assemblies != null)
            {
                foreach (KeyValuePair<string, Assembly> pair in _assemblies)
                {
                    if (pair.Value.GetType(className) is not null)
                    {
                        return pair.Value.FullName;
                    }
                }
            }

            return null;
        }

        //-----------------------------------------------
        // Private methods
        //-----------------------------------------------
        // get the type in a specified assembly
        private Type? GetType(string assemblyName, string className)
        {
            Debug.Assert(className != null, "classname can't be null");
            Debug.Assert(assemblyName != null, "Assembly name can't be null");

            // combine assembly name and class name for unique indexing
            string fullName = assemblyName + ":" + className;
            Type? type;

            if (_typeCache.TryGetValue(fullName, out Type? value))
            {
                // we found it in the cache, so just return
                return value;
            }            

            // we didn't find it in the table. So let's get to the assembly first
            Assembly? assembly = null;
            if (_assemblies is not null && _assemblies.TryGetValue(assemblyName, out Assembly? tempValue))
            {
                // find the assembly in the hash table first
                assembly = tempValue;
            }

            if (assembly == null)
            {
                // we don't find the assembly in the hashtable
                // try to use the default well known assemblies
                int index;                
                if ((index = Array.BinarySearch(_wellKnownAssemblyNames, GetAssemblyShortName(assemblyName).ToLower(CultureInfo.InvariantCulture))) >= 0)
                {
                    // see if we already loaded the assembly
                    if (_wellKnownAssemblies[index] == null)
                    {
                        // it is a well known name, load it from the gac
                        _wellKnownAssemblies[index] = Assembly.Load(assemblyName);
                    }         
                    
                    assembly = _wellKnownAssemblies[index];                                                            
                }                
            }

            if (assembly != null) 
            {
                // So, we found the assembly. Now get the type from the assembly
                type = assembly.GetType(className);   
            }
            else
            {
                // Couldn't find the assembly. We will try to load it
                type = null;
            }

            // remember what we found out.
            _typeCache[fullName] = type;
            return type;    // return
        }

        // returns the short name for the assembly
        private static string GetAssemblyShortName(string assemblyFullName)
        {
            int commaIndex = assemblyFullName.IndexOf(',');
            if (commaIndex > 0)
            {
                return assemblyFullName.Substring(0, commaIndex);
            }

            return assemblyFullName;
        }    

        private static string GetCompatibleAssemblyName(string shortName)
        {
            AssemblyName? asmName = null;
            for (int i = 0; i < _wellKnownAssemblies.Length; i++)
            {                
                if (_wellKnownAssemblies[i] != null)
                {
                    // create an assembly name with the same version and token info
                    // as the avalon assembilies
                    asmName = _wellKnownAssemblies[i].GetName();
                    asmName.Name = shortName;
                    break;
                }
            }

            if (asmName == null)
            {
                // there is no avalon assembly loaded yet. We will just get the compatible version 
                // of the current PresentationFramework
                Assembly presentationFramework = typeof(BamlLocalizer).Assembly;
                asmName = presentationFramework.GetName();
                asmName.Name = shortName;                
            }

            return asmName.ToString();
        }       

        /// <summary>
        /// gets the localizabiity attribute of a given the type
        /// </summary>        
        private static LocalizabilityAttribute? GetLocalizabilityFromType(Type? type)
        {                 
            if (type == null)
                return null;

            // inherit: true means we search in the base class as well
            object[] locAttributes = type.GetCustomAttributes(TypeOfLocalizabilityAttribute, true);                
            if (locAttributes.Length == 0)
            {
                return DefaultAttributes.GetDefaultAttribute(type);
            }                    
            else                
            {
                Debug.Assert(locAttributes.Length == 1, "Should have only 1 localizability attribute");
                   
                // use the one defined on the class
                return (LocalizabilityAttribute) locAttributes[0];
            }                     
        }

        /// <summary>
        /// Get the localizability of a CLR property
        /// </summary>
        private static void GetLocalizabilityForClrProperty(string propertyName, Type owner, out LocalizabilityAttribute? localizability, out Type? propertyType)
        {
            localizability = null;
            propertyType   = null;
            
            PropertyInfo? info = owner.GetProperty(propertyName);
            if (info == null)
            {
                return; // couldn't find the Clr property
            }

            // we found the CLR property, set the type of the property
            propertyType = info.PropertyType;

            // inherit: true means we search in the base class as well
            object[] locAttributes = info.GetCustomAttributes(TypeOfLocalizabilityAttribute, inherit: true);
            if (locAttributes.Length == 0)
            {
                return;
            }
            else
            {
                Debug.Assert(locAttributes.Length == 1, "Should have only 1 localizability attribute");

                // we found the attribute defined on the property
                localizability = (LocalizabilityAttribute)locAttributes[0];                                
            }                                
        }      

        /// <summary>
        /// Get localizability for attached property
        /// </summary>
        /// <param name="propertyName">property name</param>
        /// <param name="owner">owner type</param>
        /// <param name="localizability">out: localizability attribute</param>
        /// <param name="propertyType">out: type of the property</param>
        private static void GetLocalizabilityForAttachedProperty(string propertyName, Type owner, out LocalizabilityAttribute? localizability, out Type? propertyType)
        {
            localizability = null;
            propertyType   = null;
        
            // if it is an attached property, it should have a dependency property with the name 
            // <attached proeprty's name> + "Property"
            DependencyProperty? attachedDp = DependencyPropertyFromName(propertyName, owner);
                       
            if (attachedDp == null)
                return;  // couldn't find the dp.

            // we found the Dp, set the type of the property
            propertyType = attachedDp.PropertyType;

            FieldInfo? fieldInfo = attachedDp.OwnerType.GetField(
                attachedDp.Name + "Property", 
                BindingFlags.Public | BindingFlags.NonPublic | 
                BindingFlags.Static | BindingFlags.FlattenHierarchy);

            Debug.Assert(fieldInfo != null);

            // inherit: true means we search in the base class as well
            object[] attributes = fieldInfo.GetCustomAttributes(TypeOfLocalizabilityAttribute, inherit: true);                
            if (attributes.Length == 0)
            {
                // didn't find it.
                return;
            }
            else
            {
                Debug.Assert(attributes.Length == 1, "Should have only 1 localizability attribute");
                localizability = (LocalizabilityAttribute)attributes[0];                
            }                       
        }

        private static DependencyProperty? DependencyPropertyFromName(string propertyName, Type propertyType)
        {
            FieldInfo? fi = propertyType.GetField(propertyName + "Property", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return fi is not null ? fi.GetValue(null) as DependencyProperty : null;
        }

        //---------------------------
        // private members
        //---------------------------     
        private readonly Dictionary<string, Assembly> _assemblies;   // the _assemblies table
        private readonly Dictionary<string, Type?>     _typeCache;   // the types cache
        
        // well know assembly names, keep them sorted.
        private static readonly string[] _wellKnownAssemblyNames = new string[] { 
                "presentationcore", 
                "presentationframework",
                "windowsbase"
             };

        // the well known assemblies
        private static readonly Assembly[] _wellKnownAssemblies = new Assembly[_wellKnownAssemblyNames.Length]; 
        
        private static readonly Type TypeOfLocalizabilityAttribute = typeof(LocalizabilityAttribute);  
 
        // supported elements that are formatted inline		
        private static readonly string[] FormattedElements = new string[]{
            "System.Windows.Documents.Bold",
            "System.Windows.Documents.Hyperlink", 
            "System.Windows.Documents.Inline", 
            "System.Windows.Documents.Italic", 
            "System.Windows.Documents.SmallCaps", 
            "System.Windows.Documents.Subscript", 
            "System.Windows.Documents.Superscript", 
            "System.Windows.Documents.Underline"
            };

        // corresponding tag
        private static readonly string[] FormattingTag = new string[] {
            "b",
            "a",
            "in", 
            "i",
            "small",
            "sub",
            "sup",
            "u"
            };
    }
}
