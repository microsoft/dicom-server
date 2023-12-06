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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public static class InstanceStoreExtensions
{
    public static async Task<IReadOnlyList<InstanceMetadata>> GetInstancesWithProperties(
            this IInstanceStore instanceStore,
            IFileStore blobFileStore,
            ResourceType resourceType,
            Partition partition,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            bool isOriginalVersionRequested,
            CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
        EnsureArg.IsNotNull(blobFileStore, nameof(blobFileStore));

        IReadOnlyList<InstanceMetadata> instancesToRetrieve = await instanceStore.GetInstanceIdentifierWithPropertiesAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);

        if (!instancesToRetrieve.Any())
        {
            ThrowNotFoundException(resourceType);
        }
        
        // backfill each instance wit content length if none present and using external store
        if (instancesToRetrieve.Count > 0 && instancesToRetrieve[0].InstanceProperties.FileProperties != null)
        {

            foreach (InstanceMetadata instance in instancesToRetrieve)
            {
                if (instance.InstanceProperties.FileProperties.ContentLength == 0)
                {
                    instance.InstanceProperties.FileProperties.ContentLength = (await blobFileStore.GetFilePropertiesAsync(instance.GetVersion(isOriginalVersionRequested), partition, instance.InstanceProperties.FileProperties, cancellationToken)).ContentLength;
                }
            }
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
