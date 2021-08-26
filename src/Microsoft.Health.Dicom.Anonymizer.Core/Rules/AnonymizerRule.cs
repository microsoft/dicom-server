// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Microsoft.Health.Dicom.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public abstract class AnonymizerRule
    {
        private readonly IAnonymizerProcessor _processor;

        public AnonymizerRule(string method, string description, IAnonymizerProcessorFactory processorFactory, JObject ruleSetting = null)
        {
            EnsureArg.IsNotNull(method, nameof(method));
            EnsureArg.IsNotNull(description, nameof(description));
            EnsureArg.IsNotNull(processorFactory, nameof(processorFactory));

            Description = description;
            processorFactory ??= new DicomProcessorFactory();
            _processor = processorFactory.CreateProcessor(method, ruleSetting);
        }

        public string Description { get; set; }

        public void Handle(DicomDataset dataset, ProcessContext context)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(context, nameof(context));

            var locatedItems = LocateDicomTag(dataset, context);

            foreach (var item in locatedItems)
            {
                if (_processor.IsSupported(item))
                {
                    _processor.Process(dataset, item, context);
                    context.VisitedNodes.Add(item.ToString());
                }
                else
                {
                    throw new AnonymizerOperationException(DicomAnonymizationErrorCode.UnsupportedAnonymizationMethod, $"Rule {Description} is not supported for the item with VR {item.ValueRepresentation}.");
                }
            }
        }

        public abstract List<DicomItem> LocateDicomTag(DicomDataset dataset, ProcessContext context);
    }
}
