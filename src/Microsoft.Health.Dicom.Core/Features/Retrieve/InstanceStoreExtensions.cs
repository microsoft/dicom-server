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
                ThrowNotFoundException(resourceType);
            }

            return instancesToRetrieve;
        }

        /// <summary>
        /// Get ETag for Resource Type.
        /// If resource type is not valid or if resource Uid is not found, empty string will be returned.
        /// </summary>
        /// <param name="instanceStore">Instance store.</param>
        /// <param name="resourceType">Resource type. Valid resource types include Study, Series, and Instance.</param>
        /// <param name="uid">Uid of the respective resource.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>ETag.</returns>
        public static async Task<string> GetETag(
            this IInstanceStore instanceStore,
            ResourceType resourceType,
            string uid,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            string eTag = string.Empty;

            switch (resourceType)
            {
                case ResourceType.Study:
                    eTag = await instanceStore.GetETagForStudyAsync(uid, cancellationToken);
                    break;
                case ResourceType.Series:
                    eTag = await instanceStore.GetETagForSeriesAsync(uid, cancellationToken);
                    break;
                case ResourceType.Instance:
                    eTag = await instanceStore.GetETagForInstanceAsync(uid, cancellationToken);
                    break;
                case ResourceType.Frames:
                default:
                    break;
            }

            return eTag;
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
}
