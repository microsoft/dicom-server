// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Models
{
    public class DicomItemWrapper
    {
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

            // Date Time
            { DicomVRCode.DT, GetDateTime },
            { DicomVRCode.TM, GetDateTime },
        };

        public DicomItemWrapper(DicomItem dicomItem, DicomAttributeId attributeId)
        {
            DicomItem = dicomItem;
            AttributeId = attributeId;
        }

        public DicomItem DicomItem { get; }

        public DicomAttributeId AttributeId { get; }

        public DicomVR VR { get => DicomItem.ValueRepresentation; }

        public object GetValue()
        {
            return ValueGetters[DicomItem.ValueRepresentation.Code].Invoke(DicomItem);
        }

        public static IEnumerable<DicomItemWrapper> GetDicomItemWrappers(DicomDataset dicomDataset)
        {
            return GetCustomTagsImp(dicomDataset);
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

        private static List<DicomItemWrapper> GetCustomTagsImp(DicomDataset dataset)
        {
            List<DicomItemWrapper> result = new List<DicomItemWrapper>();
            ProcessDataset(dataset, new List<DicomTag>(), result);
            return result;
        }

        private static void ProcessDataset(DicomDataset dataset, List<DicomTag> paths, List<DicomItemWrapper> list)
        {
            foreach (var item in dataset)
            {
                ProcessItem(item, paths, list);
            }
        }

        private static void ProcessSequence(DicomSequence sequence, List<DicomTag> paths, List<DicomItemWrapper> list)
        {
            foreach (var item in sequence)
            {
                ProcessDataset(item, paths, list);
            }
        }

        private static void ProcessItem(DicomItem item, List<DicomTag> paths, List<DicomItemWrapper> list)
        {
            paths.Add(item.Tag);

            if (item is DicomSequence)
            {
                ProcessSequence((DicomSequence)item, paths, list);
            }
            else
            {
                list.Add(new DicomItemWrapper(item, new DicomAttributeId(new List<DicomTag>(paths))));
            }

            paths.RemoveAt(paths.Count - 1);
        }
    }
}
