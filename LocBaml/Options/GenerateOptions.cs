// Created on 5th Oct 2024
// by h3xds1nz

using System.Globalization;
using System;

namespace BamlLocalization.Options
{
    /// <summary>
    /// Encapsulates the necessary options generate assemblies from translation files.
    /// </summary>
    internal sealed class GenerateOptions : LocBamlOptions
    {
        /// <summary>
        /// The file from which we will load the translations for the .baml files.
        /// </summary>
        internal string Translations { get; }

        /// <summary>
        /// The type of the file which we will load the translations.
        /// </summary>
        internal FileType TranslationsSourceType { get; }

        /// <summary>
        /// The <see cref="CultureInfo"/> that the satellite assembly will be generated in.
        /// </summary>
        internal CultureInfo TargetCulture { get; }

        public GenerateOptions(LocBamlCreationOptions creationOptions) : base(creationOptions)
        {
            if (creationOptions.Translations is null)
                throw new ArgumentNullException(nameof(creationOptions));

            if (creationOptions.ToParse)
                throw new ArgumentException("Parsing options passed in", nameof(creationOptions));

            TranslationsSourceType = creationOptions.TranslationFileType;
            Translations = creationOptions.Translations;
            TargetCulture = creationOptions.CultureInfo;
        }
    }
}
