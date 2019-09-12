// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Transactional.Features.Storage.Models
{
    internal class TransactionMessage
    {
        public static readonly Encoding MessageEncoding = Encoding.UTF8;

        [JsonProperty("dicomInstances")]
        private readonly HashSet<DicomInstance> _dicomInstances;

        [JsonConstructor]
        public TransactionMessage(HashSet<DicomInstance> dicomInstances)
        {
            EnsureArg.IsNotNull(dicomInstances, nameof(dicomInstances));

            _dicomInstances = dicomInstances;
        }

        public TransactionMessage(DicomInstance dicomInstance)
            : this(new HashSet<DicomInstance>() { dicomInstance })
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));
        }

        [JsonIgnore]
        public IEnumerable<DicomInstance> DicomInstances => _dicomInstances;

        public bool AddInstance(DicomInstance dicomInstance)
        {
            EnsureArg.IsNotNull(dicomInstance, nameof(dicomInstance));
            return _dicomInstances.Add(dicomInstance);
        }
    }
}
