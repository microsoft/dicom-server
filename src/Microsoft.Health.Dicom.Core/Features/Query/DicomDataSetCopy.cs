// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class DicomDataSetCopy
    {
        private DicomDataset _dataset;
        private Func<DicomItem, DicomAttributeId, bool> _filter;

        public DicomDataSetCopy(DicomDataset dataSet, Func<DicomItem, DicomAttributeId, bool> filter)
        {
            _dataset = dataSet;
            _filter = filter;
        }

        public DicomDataset Copy()
        {
            DicomDataset copied = Copy(_dataset, null);
            return copied == null ? new DicomDataset() : copied;
        }

        private DicomDataset Copy(DicomDataset dataset, DicomAttributeId parentId)
        {
            List<DicomItem> dicomItems = new List<DicomItem>();
            foreach (DicomItem dicomItem in dataset)
            {
                DicomAttributeId newPath = parentId == null ? new DicomAttributeId(dicomItem.Tag) : parentId.Append(dicomItem.Tag);

                if (dicomItem is DicomSequence)
                {
                    DicomSequence copied = Copy((DicomSequence)dicomItem, newPath);
                    if (copied != null)
                    {
                        dicomItems.Add(copied);
                    }
                }
                else
                {
                    if (_filter.Invoke(dicomItem, newPath))
                    {
                        dicomItems.Add(dicomItem);
                    }
                }
            }

            if (dicomItems.Count == 0)
            {
                return null;
            }

            return new DicomDataset(dicomItems);
        }

        private DicomSequence Copy(DicomSequence sequence, DicomAttributeId sequenceId)
        {
            List<DicomDataset> list = new List<DicomDataset>();
            foreach (DicomDataset dataset in sequence)
            {
                DicomDataset copied = Copy(dataset, sequenceId);
                if (copied != null)
                {
                    list.Add(copied);
                }
            }

            if (list.Count == 0)
            {
                return null;
            }

            return new DicomSequence(sequence.Tag, list.ToArray());
        }
    }
}
