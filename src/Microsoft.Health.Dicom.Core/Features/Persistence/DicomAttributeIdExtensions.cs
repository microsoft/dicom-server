// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public static class DicomAttributeIdExtensions
    {
        public static bool TryGetValues<TItem>(this DicomDataset dicomDataset, DicomAttributeId attributeId, out TItem[] values)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));

            var result = new List<TItem>();
            dicomDataset.TryAppendValuesToList(result, attributeId);

            values = result.Count > 0 ? result.ToArray() : null;
            return result.Count > 0;
        }

        private static void TryAppendValuesToList<TItem>(
            this DicomDataset dicomDataset,
            List<TItem> list,
            DicomAttributeId attributeId,
            int startAttributeIndex = 0)
        {
            EnsureArg.IsGte(startAttributeIndex, 0, nameof(startAttributeIndex));

            // Handle for now sequence elements (last item in attribute ID).
            if (startAttributeIndex == attributeId.Length - 1)
            {
                // Now first validate this DICOM tag exists in the dataset.
                // Note: GetValueCount throws exception when the tag does not exist.
                if (dicomDataset.TryGetValue(attributeId.InstanceDicomTag, 0, out TItem firstItem))
                {
                    list.Add(firstItem);

                    for (int i = 1; i < dicomDataset.GetValueCount(attributeId.InstanceDicomTag); i++)
                    {
                        if (dicomDataset.TryGetValue(attributeId.InstanceDicomTag, i, out TItem value))
                        {
                            list.Add(value);
                        }
                    }
                }
            }
            else if (dicomDataset.TryGetSequence(attributeId.GetDicomTag(startAttributeIndex), out DicomSequence dicomSequence))
            {
                foreach (DicomDataset sequenceDataset in dicomSequence.Items)
                {
                    sequenceDataset.TryAppendValuesToList(list, attributeId, startAttributeIndex + 1);
                }
            }
        }
    }
}
