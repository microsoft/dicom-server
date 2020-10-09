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

        private static readonly Dictionary<string, Func<DicomItem, object>> ValueGetters = new Dictionary<string, Func<DicomItem, object>>()
        {
            // String
            { DicomVRCode.AE, GetString },
            { DicomVRCode.CS, GetString },
            { DicomVRCode.LT, GetString },
            { DicomVRCode.PN, GetString },
            { DicomVRCode.SH, GetString },
            { DicomVRCode.ST, GetString },
            { DicomVRCode.UI, GetString },
            { DicomVRCode.LO, GetString },

            // Big Int
            { DicomVRCode.AS, GetBigInt },
            { DicomVRCode.AT, GetBigInt },
            { DicomVRCode.IS, GetBigInt },
            { DicomVRCode.SL, GetBigInt },
            { DicomVRCode.SS, GetBigInt },
            { DicomVRCode.UL, GetBigInt },
            { DicomVRCode.US, GetBigInt },

            // Decimal
            { DicomVRCode.DS, GetDecimal },
            { DicomVRCode.FL, GetDecimal },
            { DicomVRCode.FD, GetDecimal },
            { DicomVRCode.DT, GetDateTime },
            { DicomVRCode.TM, GetDateTime },
        };

        public DicomCustomTag(DicomItem dicomItem, DicomAttributeId attributeId)
        {
            _dicomItem = dicomItem;
            AttributeId = attributeId;
        }

        public DicomAttributeId AttributeId { get; }

        public DicomVR VR { get => _dicomItem.ValueRepresentation; }

        public object GetValue()
        {
            return ValueGetters[_dicomItem.ValueRepresentation.Code].Invoke(_dicomItem);
        }

        public static List<DicomCustomTag> GetCustomTags(DicomDataset dataset)
        {
            DicomDataset trimedDataset = dataset.CopyWithoutBulkDataItems(true);
            return GetCustomTagsImp(trimedDataset);
        }

        private static object GetString(DicomItem item)
        {
            return ((DicomElement)item).Get<string>();
        }

        private static object GetBigInt(DicomItem item)
        {
            return ((DicomElement)item).Get<long>();
        }

        private static object GetDecimal(DicomItem item)
        {
            return ((DicomElement)item).Get<decimal>();
        }

        private static object GetDateTime(DicomItem item)
        {
            return ((DicomElement)item).Get<DateTime>();
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
            paths.Add(item.Tag);

            if (item is DicomSequence)
            {
                ProcessSequence((DicomSequence)item, paths, list);
            }
            else
            {
                list.Add(new DicomCustomTag(item, new DicomAttributeId(new List<DicomTag>(paths))));
            }

            paths.RemoveAt(paths.Count - 1);
        }
    }
}
