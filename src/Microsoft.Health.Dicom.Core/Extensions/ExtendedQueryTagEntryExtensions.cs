// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="GetExtendedQueryTagEntry"/>.
    /// </summary>
    internal static class ExtendedQueryTagEntryExtensions
    {
        /// <summary>
        /// Normalize extended query tag entry before saving to ExtendedQueryTagStore.
        /// </summary>
        /// <param name="extendedQueryTagEntry">The extended query tag entry.</param>
        /// <param name="status"> The status to set on the extended query tag entry.</param>
        /// <returns>Normalize extended query tag entry.</returns>
        public static ExtendedQueryTagStoreEntry Normalize(this AddExtendedQueryTagEntry extendedQueryTagEntry, ExtendedQueryTagStatus status)
        {
            DicomTagParser dicomTagParser = new DicomTagParser();
            DicomTag[] tags;
            if (!dicomTagParser.TryParse(extendedQueryTagEntry.Path, out tags, supportMultiple: false))
            {
                // not a valid dicom tag path
                throw new ExtendedQueryTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, extendedQueryTagEntry));
            }

            DicomTag tag = tags[0];
            string path = tag.GetPath();
            string vr = extendedQueryTagEntry.VR;
            string privateCreator = string.IsNullOrWhiteSpace(extendedQueryTagEntry.PrivateCreator) ? null : extendedQueryTagEntry.PrivateCreator;

            // when VR is not specified for known tags
            if (tag.DictionaryEntry != DicomDictionary.UnknownTag)
            {
                if (string.IsNullOrWhiteSpace(vr))
                {
                    vr = tag.GetDefaultVR()?.Code;
                }
            }

            vr = vr?.ToUpperInvariant();

            return new ExtendedQueryTagStoreEntry(-1,  path, vr, privateCreator, extendedQueryTagEntry.Level.GetValueOrDefault(), status);
        }
    }
}
