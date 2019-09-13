// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage.Models
{
    internal class TransactionMessage : ITransactionMessage
    {
        public static readonly Encoding MessageEncoding = Encoding.UTF8;

        [JsonProperty("dicomInstances")]
        private readonly HashSet<DicomInstance> _dicomInstances = new HashSet<DicomInstance>();

        [JsonConstructor]
        public TransactionMessage(DicomSeries dicomSeries, HashSet<DicomInstance> dicomInstances)
        {
            EnsureArg.IsNotNull(dicomSeries, nameof(dicomSeries));
            EnsureArg.IsNotNull(dicomInstances, nameof(dicomInstances));

            DicomSeries = dicomSeries;

            foreach (DicomInstance instance in dicomInstances)
            {
                // Use the add instance method to validate all instances in the HashSet belong to the provided series.
                AddInstance(instance);
            }
        }

        [JsonIgnore]
        public IEnumerable<DicomInstance> Instances => _dicomInstances;

        [JsonProperty("dicomSeires")]
        public DicomSeries DicomSeries { get; }

        public bool AddInstance(DicomInstance dicomInstance)
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));
            EnsureArg.IsEqualTo(dicomInstance.StudyInstanceUID, DicomSeries.StudyInstanceUID, $"This instance does not belong to the StudyInstanceUID: '{DicomSeries.StudyInstanceUID}'");
            EnsureArg.IsEqualTo(dicomInstance.SeriesInstanceUID, DicomSeries.SeriesInstanceUID, $"This instance does not belong to the SeriesInstanceUID: '{DicomSeries.SeriesInstanceUID}'");
            return _dicomInstances.Add(dicomInstance);
        }
    }
}
