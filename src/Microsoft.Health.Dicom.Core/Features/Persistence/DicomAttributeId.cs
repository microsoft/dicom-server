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
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    /// <summary>
    /// Implements the DICOM attribute parsing and serialization as defined here: http://dicom.nema.org/Dicom/2013/output/chtml/part18/sect_6.7.html.
    /// The order of tags are important in the class, all items but the last must be sequence elements.
    /// This class supports serialization/ deserialization from keywords or Hex Group/ Element.
    /// Examples of valid values for {attributeID}:
    ///     0020000D
    ///     StudyInstanceUID
    ///     00101002.00100020
    ///     OtherPatientIDsSequence.PatientID
    ///     00101002.00100024.00400032
    ///     OtherPatientIDsSequence.IssuerOfPatientIDQualifiersSequence.UniversalEntityID
    /// </summary>
    public class DicomAttributeId
    {
        private const char Seperator = '.';
        private const string GroupElementStringFormat = "X4";
        private const NumberStyles GroupElementNumberStyles = NumberStyles.HexNumber;
        private const StringComparison _stringComparison = StringComparison.InvariantCulture;
        private static readonly IFormatProvider FormatProvider = CultureInfo.InvariantCulture;
        private readonly bool _writeTagsAsKeywords = false;
        private readonly DicomTag[] _dicomTags;
        private static readonly IDictionary<string, DicomTag> KnownKeywordDicomTags = DicomDictionary.Default.ToDictionary(x => x.Keyword, x => x.Tag);

        [JsonConstructor]
        public DicomAttributeId(string attributeId)
        {
            _dicomTags = DeserializeAttributeId(attributeId);
            Validate();

            AttributeId = attributeId;
        }

        public DicomAttributeId(params DicomTag[] dicomTags)
        {
            EnsureArg.IsNotNull(dicomTags, nameof(dicomTags));
            EnsureArg.IsTrue(dicomTags.Length > 0, nameof(dicomTags));

            _dicomTags = dicomTags;
            Validate();

            AttributeId = string.Join(Seperator, dicomTags.Select(x => ConvertToString(x, _writeTagsAsKeywords)));
        }

        public string AttributeId { get; }

        [JsonIgnore]
        public int Length => _dicomTags.Length;

        /// <summary>
        /// Gets the last DICOM tag.
        /// </summary>
        [JsonIgnore]
        public DicomTag FinalDicomTag => _dicomTags[Length - 1];

        public DicomTag GetDicomTag(int index)
        {
            EnsureArg.IsGte(index, 0, nameof(index));
            EnsureArg.IsLt(index, _dicomTags.Length, nameof(index));
            return _dicomTags[index];
        }

        public override int GetHashCode()
        {
            return AttributeId.GetHashCode(_stringComparison);
        }

        public override bool Equals(object obj)
        {
            if (obj is DicomAttributeId instance && instance.Length == Length)
            {
                // Validate all the DICOM tag keywords are the same
                for (var i = 0; i < _dicomTags.Length; i++)
                {
                    if (!string.Equals(_dicomTags[i].DictionaryEntry.Keyword, instance._dicomTags[i].DictionaryEntry.Keyword, _stringComparison))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private static DicomTag[] DeserializeAttributeId(string attributeId)
        {
            EnsureArg.IsNotNullOrWhiteSpace(attributeId, nameof(attributeId));

            string[] split = attributeId.Split(Seperator);
            var result = new DicomTag[split.Length];

            for (int i = 0; i < split.Length; i++)
            {
                // First attempt Hex decimal parse of the string if correct length.
                if (split[i].Length == 8 &&
                    ushort.TryParse(split[i].Substring(0, 4), GroupElementNumberStyles, FormatProvider, out ushort group) &&
                    ushort.TryParse(split[i].Substring(4, 4), GroupElementNumberStyles, FormatProvider, out ushort element))
                {
                    result[i] = new DicomTag(group, element);
                }
                else if (KnownKeywordDicomTags.TryGetValue(split[i], out DicomTag dicomTag))
                {
                    result[i] = dicomTag;
                }
                else
                {
                    throw new FormatException($"Could not fromat the attribute '{split[i]}' from the provided attribute string '{attributeId}' to a known DICOM tag.");
                }
            }

            return result;
        }

        private static string ConvertToString(DicomTag dicomTag, bool writeTagsAsKeywords)
        {
            if (writeTagsAsKeywords)
            {
                return dicomTag.DictionaryEntry.Keyword;
            }

            string groupString = dicomTag.Group.ToString(GroupElementStringFormat, FormatProvider);
            string elementString = dicomTag.Element.ToString(GroupElementStringFormat, FormatProvider);
            return groupString + elementString;
        }

        private void Validate()
        {
            // Validate all the tags but the last has a value representation of sequence.
            for (var i = 0; i < Length - 1; i++)
            {
                if (!_dicomTags[i].DictionaryEntry.ValueRepresentations.Contains(DicomVR.SQ))
                {
                    throw new FormatException($"All tags other than the last must have a value representation of 'sequence'. The provided DICOM tag {_dicomTags[i].DictionaryEntry.Keyword} does not have a 'sequence' value type.");
                }
            }

            // Note: The last tag can have any value representation including sequence.
        }
    }
}
