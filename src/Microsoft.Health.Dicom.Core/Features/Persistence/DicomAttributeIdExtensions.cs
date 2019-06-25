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
        public static bool TryGetValues<TItem>(this DicomDataset dicomDataset, DicomAttributeId attributeId, out TItem[] values, int startAttributeIndex = 0)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(attributeId, nameof(attributeId));
            EnsureArg.IsGte(startAttributeIndex, 0, nameof(startAttributeIndex));

            var nestedValues = new List<TItem>();

            // Handle for now sequence elements (last item in attribute ID).
            if (startAttributeIndex == attributeId.Length - 1)
            {
                // Now first validate this DICOM tag exists in the dataset
                if (dicomDataset.TryGetValue(attributeId.InstanceDicomTag, 0, out TItem firstItem))
                {
                    nestedValues.Add(firstItem);

                    for (int i = 1; i < dicomDataset.GetValueCount(attributeId.InstanceDicomTag); i++)
                    {
                        if (dicomDataset.TryGetValue(attributeId.InstanceDicomTag, i, out TItem value))
                        {
                            nestedValues.Add(value);
                        }
                    }
                }
            }
            else if (dicomDataset.TryGetSequence(attributeId.GetDicomTag(startAttributeIndex), out DicomSequence dicomSequence))
            {
                foreach (DicomDataset sequenceDataset in dicomSequence.Items)
                {
                    if (sequenceDataset.TryGetValues(attributeId, out TItem[] sequenceValues, startAttributeIndex + 1))
                    {
                        nestedValues.AddRange(sequenceValues);
                    }
                }
            }

            values = nestedValues.Count > 0 ? nestedValues.ToArray() : null;
            return nestedValues.Count > 0;
        }
    }
}
