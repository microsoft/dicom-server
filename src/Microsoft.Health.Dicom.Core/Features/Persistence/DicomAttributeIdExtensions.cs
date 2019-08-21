// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public static class DicomAttributeIdExtensions
    {
        public static void Add<T>(this DicomDataset dicomDataset, DicomAttributeId attributeId, params T[] values)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));
            EnsureArg.IsNotNull(values, nameof(values));

            DicomDataset dataset = GetFinalTagDataset(dicomDataset, attributeId);
            dataset.Add(attributeId.FinalDicomTag, values);
        }

        public static void AddDicomItem(this DicomDataset dicomDataset, DicomAttributeId attributeId, DicomItem dicomItem)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));
            EnsureArg.IsNotNull(dicomItem, nameof(dicomItem));
            EnsureArg.IsTrue(attributeId.FinalDicomTag == dicomItem.Tag);

            DicomDataset dataset = GetFinalTagDataset(dicomDataset, attributeId);
            dataset.Add(dicomItem);
        }

        public static bool TryGetDicomItems(this DicomDataset dicomDataset, DicomAttributeId attributeId, out DicomItem[] dicomItems)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));

            var result = new List<DicomItem>();
            dicomDataset.TryAppendValuesToList(result, attributeId);

            dicomItems = result.Count > 0 ? result.ToArray() : null;
            return result.Count > 0;
        }

        public static bool TryGetValues<TItem>(this DicomDataset dicomDataset, DicomAttributeId attributeId, out TItem[] values)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));

            var result = new List<TItem>();
            if (dicomDataset.TryGetDicomItems(attributeId, out DicomItem[] dicomItems))
            {
                IEnumerable<DicomElement> elements = dicomItems.OfType<DicomElement>();
                result.AddRange(elements.SelectMany(x => Enumerable.Range(0, x.Count).Select(y => x.Get<TItem>(y))));
            }

            values = result.Count > 0 ? result.ToArray() : null;
            return result.Count > 0;
        }

        private static DicomDataset GetFinalTagDataset(this DicomDataset dicomDataset, DicomAttributeId attributeId)
        {
            DicomDataset currentDataset = dicomDataset;
            for (var i = 0; i < attributeId.Length - 1; i++)
            {
                DicomTag dicomTag = attributeId.GetDicomTag(i);

                if (!currentDataset.TryGetSequence(dicomTag, out DicomSequence dicomSequence))
                {
                    dicomSequence = new DicomSequence(dicomTag);
                    currentDataset.Add(dicomSequence);
                }

                currentDataset = new DicomDataset();
                dicomSequence.Items.Add(currentDataset);
            }

            return currentDataset;
        }

        private static void TryAppendValuesToList(
           this DicomDataset dicomDataset,
           List<DicomItem> list,
           DicomAttributeId attributeId,
           int attributeIndex = 0)
        {
            EnsureArg.IsGte(attributeIndex, 0, nameof(attributeIndex));
            DicomTag currentDicomTag = attributeId.GetDicomTag(attributeIndex);

            if (attributeIndex + 1 == attributeId.Length)
            {
                list.AddRange(dicomDataset.Where(x => x.Tag == currentDicomTag));
            }
            else if (dicomDataset.TryGetSequence(currentDicomTag, out DicomSequence dicomSequence))
            {
                foreach (DicomDataset sequenceDataset in dicomSequence.Items)
                {
                    sequenceDataset.TryAppendValuesToList(list, attributeId, attributeIndex + 1);
                }
            }
        }
    }
}
