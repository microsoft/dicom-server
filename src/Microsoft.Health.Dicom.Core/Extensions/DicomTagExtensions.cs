// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DicomTag"/>.
    /// </summary>
    public static class DicomTagExtensions
    {
        /// <summary>
        /// Get path of given Dicom Tag.
        /// e.g:Path of Dicom tag (0008,0070) is 00080070
        /// </summary>
        /// <param name="dicomTag">The dicom tag</param>
        /// <returns>The path.</returns>
        public static string GetPath(this DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
            return dicomTag.Group.ToString("X4") + dicomTag.Element.ToString("X4");
        }

        /// <summary>
        /// Get default VR for dicom tag.
        /// </summary>
        /// <remarks>If the dicom tag is unknown tag or private tag except PrivateCreator tag (gggg,0010-00FF)<see cref="DicomDictionary.PrivateCreatorTag"/> , <see langword="null"/> is returned.</remarks>
        /// <param name="dicomTag">The dicm tag</param>
        /// <returns>The default VR.</returns>
        public static DicomVR GetDefaultVR(this DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
            if (dicomTag.DictionaryEntry == DicomDictionary.UnknownTag)
            {
                // this tag is private or invalid tag.
                return null;
            }

            return dicomTag.DictionaryEntry.ValueRepresentations.Length > 0 ? dicomTag.DictionaryEntry.ValueRepresentations[0] : null;
        }

        /// <summary>
        /// Get friendly name of dicom tag.
        /// </summary>
        /// <param name="dicomTag">The dicom tag.</param>
        /// <returns></returns>
        public static string GetFriendlyName(this DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
            return dicomTag.IsPrivate ? dicomTag.GetPath() : dicomTag.DictionaryEntry.Keyword;
        }
    }
}
