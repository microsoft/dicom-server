// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionality to parse dicom tag path
    /// </summary>
    public class DicomTagParser : IDicomTagParser
    {
        public bool TryParse(string dicomTagPath, out DicomTag[] dicomTags)
        {
            dicomTags = null;
            if (string.IsNullOrWhiteSpace(dicomTagPath))
            {
                return false;
            }

            DicomTag[] tags = ParseTag(dicomTagPath);

            if (tags.Length > 2)
            {
                throw new DicomValidationException(dicomTagPath, DicomVR.SQ, DicomCoreResource.NestedSequencesNotSupported);
            }

            var result = tags.All(x => x != null);
            dicomTags = result ? tags : null;

            return result;
        }

        private static DicomTag[] ParseTag(string dicomTagPath)
        {
            var paths = dicomTagPath.Split(".");
            var dicomTags = new List<DicomTag>();

            foreach (var path in paths)
            {
                dicomTags.Add(ParseTagFromKeywordOrNumber(path));
            }

            return dicomTags.ToArray();
        }

        private static DicomTag ParseTagFromKeywordOrNumber(string dicomTagPath)
        {
            DicomTag dicomTag = ParseStardandDicomTagKeyword(dicomTagPath);

            if (dicomTag == null)
            {
                dicomTag = ParseDicomTagNumber(dicomTagPath);
            }

            return dicomTag;
        }

        private static DicomTag ParseStardandDicomTagKeyword(string keyword)
        {
            // Try Keyword match, returns null if not found
            DicomTag dicomTag = DicomDictionary.Default[keyword];

            // We don't accept private tag from keyword
            return (dicomTag != null && dicomTag.IsPrivate) ? null : dicomTag;
        }

        private static DicomTag ParseDicomTagNumber(string s)
        {
            // When composed with number, length could only be 8
            if (s.Length != 8)
            {
                return null;
            }

            if (!ushort.TryParse(s.AsSpan(0, 4), NumberStyles.HexNumber, null, out ushort group))
            {
                return null;
            }

            if (!ushort.TryParse(s.AsSpan(4, 4), NumberStyles.HexNumber, null, out ushort element))
            {
                return null;
            }

            var dicomTag = new DicomTag(group, element);
            DicomDictionaryEntry knownTag = DicomDictionary.Default[dicomTag];

            // Check if the tag is null or unknown.
            // Tag with odd group is considered as private.
            if (knownTag == null || (!dicomTag.IsPrivate && knownTag == DicomDictionary.UnknownTag))
            {
                return null;
            }

            return dicomTag;
        }
    }
}
