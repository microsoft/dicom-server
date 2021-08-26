// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public class AnonymizerVRRule : AnonymizerRule
    {
        public AnonymizerVRRule(DicomVR vr, string method, string description, IAnonymizerProcessorFactory processorFactory, JObject ruleSetting = null)
            : base(method, description, processorFactory, ruleSetting)
        {
            EnsureArg.IsNotNull(vr, nameof(vr));

            VR = vr;
        }

        public DicomVR VR { get; set; }

        public override List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(context, nameof(context));

            var locatedItems = new List<DicomItem>();
            foreach (var item in dataset)
            {
                if (string.Equals(item.ValueRepresentation.Code, VR?.Code))
                {
                    locatedItems.Add(item);
                }
            }

            return locatedItems.Where(x => !context.VisitedNodes.Contains(x.ToString())).ToList();
        }
    }
}
