// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features
{
    public static class DicomMetadata
    {
        public static readonly HashSet<DicomVR> DicomBulkDataVR = new HashSet<DicomVR>()
        {
            DicomVR.OB,
            DicomVR.OD,
            DicomVR.OF,
            DicomVR.OL,
            DicomVR.OW,
            DicomVR.UN,
        };

        public static void RemoveBulkDataVRs(DicomDataset dicomDataset)
        {
            var tagsToRemove = new List<DicomTag>();
            GetTagsToRemove(dicomDataset, tagsToRemove);
            dicomDataset.Remove(tagsToRemove.ToArray());
        }

        private static void GetTagsToRemove(DicomDataset dicomDataset, List<DicomTag> tagsToRemove)
        {
            foreach (DicomItem item in dicomDataset)
            {
                if (item.ValueRepresentation == DicomVR.SQ && item is DicomSequence sequence)
                {
                    foreach (DicomDataset sequenceDataset in sequence.Items)
                    {
                        GetTagsToRemove(sequenceDataset, tagsToRemove);
                    }
                }
                else if (DicomBulkDataVR.Contains(item.ValueRepresentation))
                {
                    tagsToRemove.Add(item.Tag);
                }
            }
        }
    }
}
