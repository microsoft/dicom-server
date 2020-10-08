// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Models
{
    public class DicomCustomTag
    {
        private readonly DicomItem _dicomItem;

        public DicomCustomTag(DicomItem dicomItem, DicomAttributeId attributeId)
        {
            _dicomItem = dicomItem;
            AttributeId = attributeId;
        }

        public DicomAttributeId AttributeId { get; }

        public DicomVR VR { get => _dicomItem.ValueRepresentation; }

#pragma warning disable CA1822 // Mark members as static
        public object GetValue()
#pragma warning restore CA1822 // Mark members as static
        {
            throw new NotImplementedException();
        }

        public static List<DicomCustomTag> GetCustomTags(DicomDataset dataset)
        {
            DicomDataset trimedDataset = dataset.CopyWithoutBulkDataItems(true);
            return GetCustomTagsImp(trimedDataset);
        }

        private static List<DicomCustomTag> GetCustomTagsImp(DicomDataset dataset)
        {
            List<DicomCustomTag> result = new List<DicomCustomTag>();
            ProcessDataset(dataset, new List<DicomTag>(), result);
            return result;
        }

        private static void ProcessDataset(DicomDataset dataset, List<DicomTag> paths, List<DicomCustomTag> list)
        {
            foreach (var item in dataset)
            {
                ProcessItem(item, paths, list);
            }
        }

        private static void ProcessSequence(DicomSequence sequence, List<DicomTag> paths, List<DicomCustomTag> list)
        {
            foreach (var item in sequence)
            {
                ProcessDataset(item, paths, list);
            }
        }

        private static void ProcessItem(DicomItem item, List<DicomTag> paths, List<DicomCustomTag> list)
        {
            if (item is DicomSequence)
            {
                ProcessSequence((DicomSequence)item, paths, list);
            }

            list.Add(new DicomCustomTag(item, new DicomAttributeId(new List<DicomTag>(paths))));

            paths.RemoveAt(paths.Count - 1);
        }

    }
}
