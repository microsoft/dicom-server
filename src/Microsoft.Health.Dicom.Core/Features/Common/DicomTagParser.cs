// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Common
{
    /// <summary>
    /// Provides functionality to parse dicom tag path
    /// </summary>
    public static class DicomTagParser
    {
        public static bool TryParse(string dicomTagPath, out DicomTag tag)
        {
            tag = null;

            if (string.IsNullOrWhiteSpace(dicomTagPath))
            {
                return false;
            }

            tag = ParseStardandDicomTagKeyword(dicomTagPath);

            if (tag == null)
            {
                tag = ParseDicomTagNumber(dicomTagPath);
            }

            return tag != null;
        }

        private static DicomTag ParseStardandDicomTagKeyword(string keyword)
        {
            // Try Keyword match, returns null if not found
            DicomTag dicomTag = DicomDictionary.Default[keyword];

            // We don't accept private tag from keywrod
            return (dicomTag != null && dicomTag.IsPrivate) ? null : dicomTag;
        }

        private static DicomTag ParseDicomTagNumber(string s)
        {
            // When composed with number, length could only be 8
            if (s.Length != 8)
            {
                return null;
            }

            if (!ushort.TryParse(s.Substring(0, 4), NumberStyles.HexNumber, null, out ushort group))
            {
                return null;
            }

            if (!ushort.TryParse(s.Substring(4, 4), NumberStyles.HexNumber, null, out ushort element))
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
