// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public abstract class BaseRetrieveDicomResourceHandler
    {
        private readonly IDicomMetadataStore _dicomMetadataStore;

        public BaseRetrieveDicomResourceHandler(IDicomMetadataStore dicomMetadataStore)
        {
            EnsureArg.IsNotNull(dicomMetadataStore, nameof(dicomMetadataStore));

            _dicomMetadataStore = dicomMetadataStore;
        }

        protected async Task<IEnumerable<DicomInstance>> GetInstancesToRetrieve(
            ResourceType resourceType, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> retrieveInstances;
            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    retrieveInstances = new[] { new DicomInstance(studyInstanceUID, seriesInstanceUID, sopInstanceUID) };
                    break;
                case ResourceType.Series:
                    retrieveInstances = await _dicomMetadataStore.GetInstancesInSeriesAsync(studyInstanceUID, seriesInstanceUID, cancellationToken);
                    break;
                case ResourceType.Study:
                    retrieveInstances = await _dicomMetadataStore.GetInstancesInStudyAsync(studyInstanceUID, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unkown retrieve transaction type: {resourceType}", nameof(resourceType));
            }

            return retrieveInstances;
        }
    }
}
