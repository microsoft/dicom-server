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
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public abstract class BaseRetrieveDicomResourceHandler
    {
        public BaseRetrieveDicomResourceHandler(DicomDataStore dicomDataStore)
        {
            EnsureArg.IsNotNull(dicomDataStore, nameof(dicomDataStore));

            DicomDataStore = dicomDataStore;
        }

        protected DicomDataStore DicomDataStore { get; }

        public async Task<IEnumerable<DicomInstance>> GetInstancesToRetrieve(RetrieveDicomResourceRequest message, CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstance> retrieveInstances;
            switch (message.ResourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    retrieveInstances = new[] { new DicomInstance(message.StudyInstanceUID, message.SeriesInstanceUID, message.SopInstanceUID) };
                    break;
                case ResourceType.Series:
                    retrieveInstances = await DicomDataStore.GetInstancesInSeriesAsync(message.StudyInstanceUID, message.SeriesInstanceUID, cancellationToken);
                    break;
                case ResourceType.Study:
                    retrieveInstances = await DicomDataStore.GetInstancesInStudyAsync(message.StudyInstanceUID, cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unkown retrieve transaction type: {message.ResourceType}", nameof(message));
            }

            return retrieveInstances;
        }
    }
}
