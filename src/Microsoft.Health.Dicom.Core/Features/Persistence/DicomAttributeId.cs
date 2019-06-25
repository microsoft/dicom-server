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
        private static readonly IFormatProvider FormatProvider = CultureInfo.InvariantCulture;
        private readonly bool _writeTagsAsKeywords = false;
        private readonly DicomTag[] _dicomTags;
        private static readonly IDictionary<string, DicomTag> KnownKeywordDicomTags =
            typeof(DicomTag).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(x => x.GetValue(null) as DicomTag)
                .Where(x => !string.IsNullOrWhiteSpace(x?.DictionaryEntry?.Keyword))
                .Distinct(new KeywordComparer())
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

        [JsonIgnore]
        public DicomTag LeafDicomTag => _dicomTags[Length - 1];

        public DicomTag GetDicomTag(int index = -1)
        {
            EnsureArg.IsLt(index, _dicomTags.Length, nameof(index));
            return index < 0 ? _dicomTags[Length - 1] : _dicomTags[index];
        }

        public override int GetHashCode()
        {
            return AttributeId.GetHashCode(StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is DicomAttributeId instance)
            {
                return AttributeId.Equals(instance.AttributeId, StringComparison.Ordinal);
            }

            return false;
        }

        public static DicomTag[] DeserializeAttributeId(string attributeId)
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
                    continue;
                }

                // Otherwise, attempt to look-up the DICOM tag keyword.
                EnsureArg.IsTrue(KnownKeywordDicomTags.ContainsKey(split[i]), split[i]);
                result[i] = KnownKeywordDicomTags[split[i]];
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
                EnsureArg.IsTrue(_dicomTags[i].DictionaryEntry.ValueRepresentations.Contains(DicomVR.SQ));
            }

            EnsureArg.IsFalse(LeafDicomTag.DictionaryEntry.ValueRepresentations.Contains(DicomVR.SQ));
        }

        private class KeywordComparer : IEqualityComparer<DicomTag>
        {
            public bool Equals(DicomTag dicomTag1, DicomTag dicomTag2)
            {
                return dicomTag1.DictionaryEntry.Keyword == dicomTag2.DictionaryEntry.Keyword;
            }

            public int GetHashCode(DicomTag dicomTag)
            {
                return dicomTag.DictionaryEntry.Keyword.GetHashCode(StringComparison.InvariantCulture);
            }
        }
    }
}
