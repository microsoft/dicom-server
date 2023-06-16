// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public static class InstanceStoreExtensions
{
    public static async Task<IReadOnlyList<InstanceMetadata>> GetInstancesWithProperties(
            this IInstanceStore instanceStore,
            ResourceType resourceType,
            PartitionEntry partitionEntry,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));

        IReadOnlyList<InstanceMetadata> instancesToRetrieve = await instanceStore.GetInstanceIdentifierWithPropertiesAsync(partitionEntry, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);

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
                throw new InstanceNotFoundException();
            case ResourceType.Series:
                throw new InstanceNotFoundException(DicomCoreResource.SeriesInstanceNotFound);
            case ResourceType.Study:
                throw new InstanceNotFoundException(DicomCoreResource.StudyInstanceNotFound);
        }
    }
}
