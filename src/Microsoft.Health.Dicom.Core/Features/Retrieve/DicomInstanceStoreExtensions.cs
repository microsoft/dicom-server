// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public static class DicomInstanceStoreExtensions
    {
        public static async Task<IEnumerable<VersionedDicomInstanceIdentifier>> GetInstancesToRetrieve(
                this IDicomInstanceStore dicomInstanceStore,
                ResourceType resourceType,
                string studyInstanceUid,
                string seriesInstanceUid,
                string sopInstanceUid,
                CancellationToken cancellationToken)
        {
            IEnumerable<VersionedDicomInstanceIdentifier> instancesToRetrieve = Enumerable.Empty<VersionedDicomInstanceIdentifier>();

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
                ThrowNotFoundException(resourceType);
            }

            return instancesToRetrieve;
        }

        private static void ThrowNotFoundException(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Frames:
                case ResourceType.Instance:
                    throw new DicomInstanceNotFoundException();
                case ResourceType.Series:
                    throw new DicomInstanceNotFoundException(DicomCoreResource.SeriesInstanceNotFound);
                case ResourceType.Study:
                    throw new DicomInstanceNotFoundException(DicomCoreResource.StudyInstanceNotFound);
            }
        }
    }
}
