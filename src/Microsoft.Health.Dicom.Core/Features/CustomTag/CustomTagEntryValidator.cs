// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagEntryValidator : ICustomTagEntryValidator
    {
        private const string UnknownTagName = "Unknown";

        /* Unsupported VRCodes:
         LT(Long Text), OB (Other Byte), OD (Other Double), OF(Other Float), OL (Other Long), OV(other Very long), OW (other Word), ST(Short Text, SV (Signed Very long)
         UC (Unlimited Characters), UN (Unknown), UR (URI), UT (Unlimited Text), UV (Unsigned Very long)
         Note: we dont' find definition for UR, UV and SV in DICOM standard (http://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html)
        */

        public static readonly IImmutableSet<string> SupportedVRCodes = ImmutableHashSet.Create(
            DicomVRCode.AE,
            DicomVRCode.AS,
            DicomVRCode.AT,
            DicomVRCode.CS,
            DicomVRCode.DA,
            DicomVRCode.DS,
            DicomVRCode.DT,
            DicomVRCode.FD,
            DicomVRCode.FL,
            DicomVRCode.IS,
            DicomVRCode.LO,
            DicomVRCode.PN,
            DicomVRCode.SH,
            DicomVRCode.SL,
            DicomVRCode.SS,
            DicomVRCode.TM,
            DicomVRCode.UI,
            DicomVRCode.UL,
            DicomVRCode.US);

        public void ValidateCustomTags(IEnumerable<CustomTagEntry> customTagEntries)
        {
            EnsureArg.IsNotNull(customTagEntries, nameof(customTagEntries));
            if (customTagEntries.Count() == 0)
            {
                throw new CustomTagEntryValidationException(DicomCoreResource.MissingCustomTag);
            }

            HashSet<string> pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CustomTagEntry tagEntry in customTagEntries)
            {
                ValidateCustomTagEntry(tagEntry);

                // don't allow duplicated path
                if (pathSet.Contains(tagEntry.Path))
                {
                    throw new CustomTagEntryValidationException(
                         string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DuplicateCustomTag, tagEntry.Path));
                }

                pathSet.Add(tagEntry.Path);
            }
        }

        /// <summary>
        /// Validate custom tag entry.
        /// </summary>
        /// <param name="tagEntry">the tag entry.</param>
        private void ValidateCustomTagEntry(CustomTagEntry tagEntry)
        {
            DicomTag tag = ParseTag(tagEntry.Path);

            // cannot be any tag we already support
            if (QueryLimit.AllInstancesTags.Contains(tag))
            {
                throw new CustomTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.CustomTagAlreadySupported, tag.DictionaryEntry.Name));
            }

            if (tag.IsPrivate)
            {
                // this is private tag, VR is required
                ParseVRCode(tagEntry.VR);
                EnsureVRIsSupported(tagEntry.VR);
            }
            else
            {
                // stardard tag must have name - should not be "Unknown".
                if (tag.DictionaryEntry.Name.Equals(UnknownTagName, StringComparison.OrdinalIgnoreCase))
                {
                    // not a valid dicom tag
                    throw new CustomTagEntryValidationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, tag));
                }

                if (string.IsNullOrWhiteSpace(tagEntry.VR))
                {
                    // When VR is missing for standard tag, still need to verify VRCode
                    string vrCode = tag.DictionaryEntry.ValueRepresentations[0].Code;
                    EnsureVRIsSupported(vrCode);
                }
                else
                {
                    // when VR is specified, verify it's correct
                    // parse VR
                    DicomVR vr = ParseVRCode(tagEntry.VR);

                    EnsureVRIsSupported(vr.Code);

                    if (!tag.DictionaryEntry.ValueRepresentations.Contains(vr))
                    {
                        // not a valid VR
                        throw new CustomTagEntryValidationException(
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedVRCodeOnTag, vr.Code, tag));
                    }
                }
            }
        }

        private static DicomVR ParseVRCode(string vrCode)
        {
            if (string.IsNullOrWhiteSpace(vrCode))
            {
                throw new CustomTagEntryValidationException(DicomCoreResource.MissingVRCode);
            }

            try
            {
                // DicomVR.Parse only accept upper case  VR code.
                return DicomVR.Parse(vrCode.ToUpper(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                throw new CustomTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidVRCode, vrCode), ex);
            }
        }

        private static DicomTag ParseTag(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new CustomTagEntryValidationException(DicomCoreResource.MissingCustomTag);
            }

            try
            {
                return DicomTag.Parse(path);
            }
            catch (Exception ex)
            {
                throw new CustomTagEntryValidationException(
                      string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, path), ex);
            }
        }

        private static void EnsureVRIsSupported(string vrCode)
        {
            if (!SupportedVRCodes.Contains(vrCode))
            {
                throw new CustomTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedVRCode, vrCode));
            }
        }
    }
}
