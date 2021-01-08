// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
                    ParseVR(tagEntry.VR);
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

                    if (!string.IsNullOrWhiteSpace(tagEntry.VR))
                    {
                        // when VR is specified, verify it's correct
                        // parse VR
                        DicomVR vr = ParseVR(tagEntry.VR);
                        if (!tag.DictionaryEntry.ValueRepresentations.Contains(vr))
                        {
                            // not a valid VR
                            throw new CustomTagEntryValidationException(
                                string.Format(CultureInfo.InvariantCulture, DicomCoreResource.UnsupportedVRCode, vr.Code, tag));
                        }
                    }

                    // Since we are able to infer VR for standard tag, user don't need to specify
                }

                // don't allow duplicated path
                if (pathSet.Contains(tagEntry.Path))
                {
                    throw new CustomTagEntryValidationException(
                         string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DuplicateCustomTag, tag));
                }

                pathSet.Add(tagEntry.Path);
            }
        }

        private DicomVR ParseVR(string vrCode)
        {
            if (string.IsNullOrWhiteSpace(vrCode))
            {
                throw new CustomTagEntryValidationException(DicomCoreResource.MissingVRCode);
            }

            try
            {
                return DicomVR.Parse(vrCode.ToUpper(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                throw new CustomTagEntryValidationException(
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidVRCode, vrCode), ex);
            }
        }

        private DicomTag ParseTag(string path)
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
    }
}
