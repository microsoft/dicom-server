// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage
{
    internal static class DicomTagSerializer
    {
        private const char Seperator = ',';
        private const string GroupElementStringFormat = "X4";
        private static readonly IFormatProvider FormatProvider = CultureInfo.InvariantCulture;

        public static string Serialize(DicomTag dicomTag)
        {
            EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));

            string groupString = dicomTag.Group.ToString(GroupElementStringFormat, FormatProvider);
            string elementString = dicomTag.Element.ToString(GroupElementStringFormat, FormatProvider);

            if (dicomTag.PrivateCreator == null)
            {
                return $"{groupString}{Seperator}{elementString}";
            }

            return $"{groupString}{Seperator}{elementString}{Seperator}{dicomTag.PrivateCreator.Creator}";
        }

        public static DicomTag Deserialize(string dictionaryElement)
        {
            EnsureArg.IsNotNullOrWhiteSpace(dictionaryElement, nameof(dictionaryElement));
            var split = dictionaryElement.Split(Seperator);

            EnsureArg.IsTrue(split.Length == 2 || split.Length == 3, nameof(dictionaryElement));
            EnsureArg.IsTrue(split[0].Length == 4, nameof(dictionaryElement));
            EnsureArg.IsTrue(split[1].Length == 4, nameof(dictionaryElement));

            EnsureArg.IsTrue(ushort.TryParse(split[0], NumberStyles.HexNumber, FormatProvider, out ushort group), nameof(dictionaryElement));
            EnsureArg.IsTrue(ushort.TryParse(split[1], NumberStyles.HexNumber, FormatProvider, out ushort element), nameof(dictionaryElement));

            if (split.Length == 2)
            {
                return new DicomTag(group, element);
            }

            return new DicomTag(group, element, split[2]);
        }
    }
}
