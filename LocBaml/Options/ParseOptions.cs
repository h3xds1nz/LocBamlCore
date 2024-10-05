// Created on 5th Oct 2024
// by h3xds1nz

using System;

namespace BamlLocalization.Options
{
    /// <summary>
    /// Encapsulates the necessary options to parse .baml streams into translations file.
    /// </summary>
    internal sealed class ParseOptions : LocBamlOptions
    {
        /// <summary>
        /// The type of the file which we will load the translations.
        /// </summary>
        internal FileType TranslationsTargetType { get; }

        public ParseOptions(LocBamlCreationOptions creationOptions) : base(creationOptions)
        {
            if (creationOptions.ToGenerate)
                throw new ArgumentException("Generation options passed in", nameof(creationOptions));

            TranslationsTargetType = creationOptions.TranslationFileType;
        }
    }
}
