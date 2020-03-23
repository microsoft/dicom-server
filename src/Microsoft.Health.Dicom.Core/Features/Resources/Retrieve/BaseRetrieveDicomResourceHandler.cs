// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Retrieve
{
    public abstract class BaseRetrieveDicomResourceHandler
    {
        private readonly IDicomInstanceService _dicomInstanceService;

        public BaseRetrieveDicomResourceHandler(IDicomInstanceService dicomInstanceService)
        {
            EnsureArg.IsNotNull(dicomInstanceService, nameof(dicomInstanceService));

            _dicomInstanceService = dicomInstanceService;
        }

        protected async Task<IEnumerable<DicomInstanceIdentifier>> GetInstancesToRetrieve(
            ResourceType resourceType,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> retrievedInstances;
            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    retrievedInstances = await _dicomInstanceService.GetInstanceIdentifierAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Series:
                    retrievedInstances = await _dicomInstanceService.GetInstanceIdentifiersInSeriesAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Study:
                    retrievedInstances = await _dicomInstanceService.GetInstanceIdentifiersInStudyAsync(
                        studyInstanceUid,
                        cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unknown retrieve transaction type: {resourceType}", nameof(resourceType));
            }

            return retrievedInstances;
        }
    }
}
