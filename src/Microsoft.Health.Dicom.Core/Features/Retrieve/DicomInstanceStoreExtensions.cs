// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public static class DicomInstanceStoreExtensions
    {
        public static async Task<IEnumerable<DicomInstanceIdentifier>> GetInstancesToRetrieve(
                this IDicomInstanceStore dicomInstanceStore,
                ResourceType resourceType,
                string studyInstanceUid,
                string seriesInstanceUid,
                string sopInstanceUid,
                CancellationToken cancellationToken)
        {
            IEnumerable<DicomInstanceIdentifier> instancesToRetrieve = Enumerable.Empty<DicomInstanceIdentifier>();
            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifierAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        sopInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Series:
                    instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifiersInSeriesAsync(
                        studyInstanceUid,
                        seriesInstanceUid,
                        cancellationToken);
                    break;
                case ResourceType.Study:
                    instancesToRetrieve = await dicomInstanceStore.GetInstanceIdentifiersInStudyAsync(
                        studyInstanceUid,
                        cancellationToken);
                    break;
                default:
                    Debug.Fail($"Unknown retrieve transaction type: {resourceType}", nameof(resourceType));
                    break;
            }

            if (!instancesToRetrieve.Any())
            {
                throw new DicomInstanceNotFoundException();
            }

            return instancesToRetrieve;
        }
    }
}
