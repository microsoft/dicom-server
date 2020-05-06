// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public static class InstanceStoreExtensions
    {
        public static async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstancesToRetrieve(
                this IInstanceStore instanceStore,
                ResourceType resourceType,
                string studyInstanceUid,
                string seriesInstanceUid,
                string sopInstanceUid,
                CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));

            IEnumerable<VersionedInstanceIdentifier> instancesToRetrieve = Enumerable.Empty<VersionedInstanceIdentifier>();

            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    instancesToRetrieve = await instanceStore.GetInstanceIdentifierAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Series:
                    instancesToRetrieve = await instanceStore.GetInstanceIdentifiersInSeriesAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Study:
                    instancesToRetrieve = await instanceStore.GetInstanceIdentifiersInStudyAsync(
                        studyInstanceUid,
                        cancellationToken);
                    break;
                default:
                    Debug.Fail($"Unknown retrieve transaction type: {resourceType}", nameof(resourceType));
                    break;
            }

            if (!instancesToRetrieve.Any())
            {
                throw new InstanceNotFoundException();
            }

            return instancesToRetrieve;
        }
    }
}
