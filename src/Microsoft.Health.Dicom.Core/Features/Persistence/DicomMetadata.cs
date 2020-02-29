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
            foreach (DicomItem item in dicomDataset)
            {
                if (item.ValueRepresentation == DicomVR.SQ && item is DicomSequence sequence)
                {
                    foreach (DicomDataset sequenceDataset in sequence.Items)
                    {
                        RemoveBulkDataVRs(sequenceDataset);
                    }
                }
                else if (DicomBulkDataVR.Contains(item.ValueRepresentation))
                {
                    tagsToRemove.Add(item.Tag);
                }
            }

            dicomDataset.Remove(tagsToRemove.ToArray());
        }
   }
}
