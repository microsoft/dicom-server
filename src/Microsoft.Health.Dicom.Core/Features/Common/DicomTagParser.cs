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
    public class DicomTagParser : IDicomTagParser
    {
        public bool TryParse(string dicomTagPath, out DicomTag[] dicomTags, bool supportMultiple = false)
        {
            dicomTags = null;
            if (supportMultiple)
            {
                throw new System.NotImplementedException("Sequential dicom tags are currently not supported.");
            }

            if (string.IsNullOrWhiteSpace(dicomTagPath))
            {
                return false;
            }

            // Try Keyword match, returns null if not found
            DicomTag dicomTag = DicomDictionary.Default[dicomTagPath];

            if (dicomTag == null)
            {
                dicomTag = ParseDicomTagNumber(dicomTagPath);
            }

            dicomTags = new DicomTag[] { dicomTag };
            return dicomTag != null;
        }

        public string ParseFormattedTagPath(string dicomTagPath, bool supportMultiple = false)
        {
            if ((!supportMultiple && dicomTagPath.Contains('.', System.StringComparison.OrdinalIgnoreCase)) || supportMultiple)
            {
                throw new System.NotImplementedException("Sequential dicom tags are currently not supported.");
            }

            return string.Join(string.Empty, dicomTagPath.Split('(', ',', ')', '.'));
        }

        private static DicomTag ParseDicomTagNumber(string s)
        {
            if (s.Length < 8)
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
