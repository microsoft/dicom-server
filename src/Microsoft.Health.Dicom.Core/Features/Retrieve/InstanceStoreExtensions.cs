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
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public static class InstanceStoreExtensions
{
    public static async Task<IReadOnlyList<InstanceMetadata>> GetInstancesWithProperties(
            this IInstanceStore instanceStore,
            ResourceType resourceType,
            Partition partition,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            bool isInitialVersion = false,
            CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));

        IReadOnlyList<InstanceMetadata> instancesToRetrieve = await instanceStore.GetInstanceIdentifierWithPropertiesAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, isInitialVersion, cancellationToken);

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
