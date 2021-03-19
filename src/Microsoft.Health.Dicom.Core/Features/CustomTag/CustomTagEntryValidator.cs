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
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagEntryValidator : ICustomTagEntryValidator
    {
        private readonly IDicomTagParser _dicomTagParser;

        public CustomTagEntryValidator(IDicomTagParser dicomTagParser)
        {
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            _dicomTagParser = dicomTagParser;
        }

        /*
         * Unsupported VRCodes:
         * LT(Long Text), OB (Other Byte), OD (Other Double), OF(Other Float), OL (Other Long), OV(other Very long), OW (other Word), ST(Short Text, SV (Signed Very long)
         * UC (Unlimited Characters), UN (Unknown), UR (URI), UT (Unlimited Text), UV (Unsigned Very long)
         * Note: we dont' find definition for UR, UV and SV in DICOM standard (http://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html)
         */
        public static IImmutableSet<string> SupportedVRCodes { get; } = ImmutableHashSet.Create(
            DicomVRCode.AE,
            DicomVRCode.AS,
            DicomVRCode.CS,
            DicomVRCode.DA,
            DicomVRCode.DS,
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
            if (!customTagEntries.Any())
            {
                throw new CustomTagEntryValidationException(DicomCoreResource.MissingCustomTag);
            }

            var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, tagEntry.Path));
            }

            ValidatePrivateCreator(tag, tagEntry.PrivateCreator, tagEntry.Path);

            if (tag.IsPrivate)
            {
                // this is private tag, VR is required
                ParseVRCode(tagEntry.VR);
                EnsureVRIsSupported(tagEntry.VR);
            }
            else
            {
                // stardard tag must have name - should not be "Unknown".
                if (tag.DictionaryEntry.Equals(DicomDictionary.UnknownTag))
                {
                    // not a valid dicom tag
                    throw new CustomTagEntryValidationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, tagEntry.Path));
                }

                if (string.IsNullOrWhiteSpace(tagEntry.VR))
                {
                    // When VR is missing for standard tag, still need to verify VRCode
                    string vrCode = tag.GetDefaultVR()?.Code;

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

        private static void ValidatePrivateCreator(DicomTag tag, string privateCreator, string originalTagPath)
        {
            if (tag.IsPrivate)
            {
                if (string.IsNullOrWhiteSpace(privateCreator))
                {
                    throw new CustomTagEntryValidationException(
                      string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingPrivateCreator, originalTagPath));
                }

                try
                {
                    DicomElementMinimumValidation.ValidateLO(privateCreator, nameof(privateCreator));
                }
                catch (DicomElementValidationException ex)
                {
                    throw new CustomTagEntryValidationException(
                       string.Format(CultureInfo.InvariantCulture, DicomCoreResource.PrivateCreatorNotValidLO, originalTagPath), ex);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(privateCreator))
                {
                    throw new CustomTagEntryValidationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.PrivateCreatorNotEmpty, originalTagPath));
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
            catch (DicomDataException ex)
            {
                throw new CustomTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidVRCode, vrCode), ex);
            }
        }

        private DicomTag ParseTag(string path)
        {
            if (!_dicomTagParser.TryParse(path, out DicomTag[] result, supportMultiple: false))
            {
                throw new CustomTagEntryValidationException(
                      string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, path));
            }

            return result[0];
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
