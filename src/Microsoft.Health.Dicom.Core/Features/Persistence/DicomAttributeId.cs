// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        private static readonly IDictionary<string, DicomTag> KnownKeywordDicomTags =
            typeof(DicomTag).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(x => x.GetValue(null) as DicomTag)
                .Where(x => !string.IsNullOrWhiteSpace(x?.DictionaryEntry?.Keyword))
                .Distinct(new KeywordComparer(_stringComparison))
                .ToDictionary(x => x.DictionaryEntry.Keyword, x => x);

        [JsonConstructor]
        public DicomAttributeId(string attributeId)
            : this(DeserializeAttributeId(attributeId))
        {
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
        /// Gets the non-sequence DICOM tag.
        /// </summary>
        [JsonIgnore]
        public DicomTag InstanceDicomTag => _dicomTags[Length - 1];

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
            // Validate all but the leaf tag are sequence elements
            for (var i = 0; i < Length - 1; i++)
            {
                if (!_dicomTags[i].DictionaryEntry.ValueRepresentations.Contains(DicomVR.SQ))
                {
                    throw new FormatException($"All tags but the last must be sequence elements. The provided DICOM tag {_dicomTags[i].DictionaryEntry.Keyword} does not have a 'sequence' value type.");
                }
            }

            if (InstanceDicomTag.DictionaryEntry.ValueRepresentations.Contains(DicomVR.SQ))
            {
                throw new FormatException($"The last DICOM tag must not have the value representation 'sequence'. The provided DICOM tag {InstanceDicomTag.DictionaryEntry.Keyword} is known as having a 'sequence' value type.");
            }
        }

        private class KeywordComparer : IEqualityComparer<DicomTag>
        {
            private readonly StringComparison _comparisonType;

            public KeywordComparer(StringComparison comparisonType)
            {
                _comparisonType = comparisonType;
            }

            public bool Equals(DicomTag dicomTag1, DicomTag dicomTag2)
            {
                return string.Equals(dicomTag1.DictionaryEntry.Keyword, dicomTag2.DictionaryEntry.Keyword, _comparisonType);
            }

            public int GetHashCode(DicomTag dicomTag)
            {
                return dicomTag.DictionaryEntry.Keyword.GetHashCode(_comparisonType);
            }
        }
    }
}
