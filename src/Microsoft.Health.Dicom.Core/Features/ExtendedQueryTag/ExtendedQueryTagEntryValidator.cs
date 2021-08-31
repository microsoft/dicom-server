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
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagEntryValidator : IExtendedQueryTagEntryValidator
    {
        private readonly IDicomTagParser _dicomTagParser;

        public ExtendedQueryTagEntryValidator(IDicomTagParser dicomTagParser)
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
            DicomVRCode.UI,
            DicomVRCode.UL,
            DicomVRCode.US);

        public void ValidateExtendedQueryTags(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries)
        {
            EnsureArg.IsNotNull(extendedQueryTagEntries, nameof(extendedQueryTagEntries));

            var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AddExtendedQueryTagEntry tagEntry in extendedQueryTagEntries)
            {
                ValidateExtendedQueryTagEntry(tagEntry);

                // don't allow duplicated path
                if (pathSet.Contains(tagEntry.Path))
                {
                    throw new ExtendedQueryTagEntryValidationException(
                         string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DuplicateExtendedQueryTag, tagEntry.Path));
                }

                pathSet.Add(tagEntry.Path);
            }

            if (pathSet.Count == 0)
            {
                throw new ExtendedQueryTagEntryValidationException(DicomCoreResource.MissingExtendedQueryTag);
            }
        }

        /// <summary>
        /// Validate extended query tag entry.
        /// </summary>
        /// <param name="tagEntry">the tag entry.</param>
        private void ValidateExtendedQueryTagEntry(AddExtendedQueryTagEntry tagEntry)
        {
            DicomTag tag = ParseTag(tagEntry.Path);

            // cannot be any tag we already support
            if (QueryLimit.CoreTags.Contains(tag))
            {
                throw new ExtendedQueryTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.QueryTagAlreadySupported, tagEntry.Path));
            }

            if (tagEntry.Level == null)
            {
                throw new ExtendedQueryTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingLevel, tagEntry.Path));
            }

            ValidatePrivateCreator(tag, tagEntry.PrivateCreator, tagEntry.Path);

            ValidateVRCode(tag, tagEntry.VR, tagEntry.Path);
        }

        private static void ValidateVRCode(DicomTag tag, string vrCode, string tagPath)
        {
            DicomVR dicomVR = string.IsNullOrWhiteSpace(vrCode) ? null : ParseVRCode(vrCode, tagPath);

            if (tag.DictionaryEntry != DicomDictionary.UnknownTag)
            {
                // if VR is specified for knownTag, validate 
                if (dicomVR != null)
                {
                    if (!tag.DictionaryEntry.ValueRepresentations.Contains(dicomVR))
                    {
                        // not a valid VR
                        throw new ExtendedQueryTagEntryValidationException(
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedVRCodeOnTag, vrCode, tagPath, tag.GetDefaultVR()));
                    }
                }
                else
                {
                    // otherwise, get default one
                    dicomVR = tag.GetDefaultVR();
                }
            }
            else
            {
                // for unknown tag, vrCode is required
                if (dicomVR == null)
                {
                    throw new ExtendedQueryTagEntryValidationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingVRCode, tagPath));
                }
            }

            EnsureVRIsSupported(dicomVR, tagPath);
        }

        private static void ValidatePrivateCreator(DicomTag tag, string privateCreator, string tagPath)
        {
            if (!tag.IsPrivate)
            {
                // Standard tags should not have private creator.
                if (!string.IsNullOrWhiteSpace(privateCreator))
                {
                    throw new ExtendedQueryTagEntryValidationException(
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.PrivateCreatorNotEmpty, tagPath));
                }
                return;
            }

            // PrivateCreator Tag should not have privateCreator.
            if (tag.DictionaryEntry == DicomDictionary.PrivateCreatorTag)
            {
                if (!string.IsNullOrWhiteSpace(privateCreator))
                {
                    throw new ExtendedQueryTagEntryValidationException(
                       string.Format(CultureInfo.InvariantCulture, DicomCoreResource.PrivateCreatorNotEmptyForPrivateIdentificationCode, tagPath));
                }
                return;
            }

            // Private tag except PrivateCreator requires privateCreator
            if (string.IsNullOrWhiteSpace(privateCreator))
            {
                throw new ExtendedQueryTagEntryValidationException(
                  string.Format(CultureInfo.InvariantCulture, DicomCoreResource.MissingPrivateCreator, tagPath));
            }

            try
            {
                LongStringValidation.Validate(privateCreator, nameof(privateCreator));
            }
            catch (ElementValidationException ex)
            {
                throw new ExtendedQueryTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.PrivateCreatorNotValidLO, tagPath), ex);
            }

        }

        private static DicomVR ParseVRCode(string vrCode, string tagPath)
        {
            try
            {
                // DicomVR.Parse only accept upper case  VR code.
                return DicomVR.Parse(vrCode.ToUpper(CultureInfo.InvariantCulture));
            }
            catch (DicomDataException ex)
            {
                throw new ExtendedQueryTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidVRCode, vrCode, tagPath), ex);
            }
        }

        private DicomTag ParseTag(string path)
        {
            if (!_dicomTagParser.TryParse(path, out DicomTag[] result, supportMultiple: false))
            {
                throw new ExtendedQueryTagEntryValidationException(
                      string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidExtendedQueryTag, path));
            }

            return result[0];
        }

        private static void EnsureVRIsSupported(DicomVR vr, string tagPath)
        {
            if (!SupportedVRCodes.Contains(vr.Code))
            {
                throw new ExtendedQueryTagEntryValidationException(
                   string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedVRCode, vr.Code, tagPath));
            }
        }
    }
}
