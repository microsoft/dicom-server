// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="CustomTagEntry"/>.
    /// </summary>
    internal static class CustomTagEntryExtensions
    {
        /// <summary>
        /// Normalize custom tag entry before saving to CustomTagStore.
        /// </summary>
        /// <param name="customTagEntry">The custom tag entry.</param>
        /// <param name="status"> The status to set on the custom tag entry.</param>
        /// <returns>Normalize custom tag entry.</returns>
        public static CustomTagEntry Normalize(this CustomTagEntry customTagEntry, CustomTagStatus status)
        {
            DicomTagParser dicomTagParser = new DicomTagParser();
            DicomTag[] tags;
            if (!dicomTagParser.TryParse(customTagEntry.Path, out tags, supportMultiple: false))
            {
                // not a valid dicom tag path
                throw new CustomTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, customTagEntry));
            }

            DicomTag tag = tags[0];
            string path = tag.GetPath();
            string vr = customTagEntry.VR;
            string privateCreator = string.IsNullOrWhiteSpace(customTagEntry.PrivateCreator) ? null : customTagEntry.PrivateCreator;

            // when VR is not specified for standard tag,
            if (!tag.IsPrivate && tag.DictionaryEntry != DicomDictionary.UnknownTag)
            {
                if (string.IsNullOrWhiteSpace(vr))
                {
                    vr = tag.GetDefaultVR()?.Code;
                }
            }

            vr = vr.ToUpperInvariant();

            return new CustomTagEntry { Path = path, VR = vr, PrivateCreator = privateCreator, Level = customTagEntry.Level, Status = status };
        }
    }
}
