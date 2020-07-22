// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class ETagHelper : IETagHelper
    {
        private readonly IInstanceStore _instanceStore;

        public ETagHelper(IInstanceStore instanceStore)
        {
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            _instanceStore = instanceStore;
        }

        /// <summary>
        /// Get ETag for resource type.
        /// Valid resource types are Study, Series, and Instance.
        /// ETag cannot be calculated for Frames.
        /// For Study and Series, ETag is calculated using the following formula:
        ///     $"{Max(Instance Watermark)}-{CounfOfInstances}"
        /// For Instance, respective Watermark serves as the ETag.
        /// </summary>
        /// <param name="resourceType">Resource type. Valid resource types include Study, Series and Instance.</param>
        /// <param name="uid">Uid of the respective resource type.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>ETag.</returns>
        public async Task<string> GetETag(ResourceType resourceType, string uid, CancellationToken cancellationToken)
        {
            string eTag = string.Empty;

            if (string.IsNullOrEmpty(uid))
            {
                return eTag;
            }

            switch (resourceType)
            {
                case ResourceType.Study:
                    eTag = await _instanceStore.GetETag(
                                    ResourceType.Study,
                                    uid,
                                    cancellationToken);
                    break;
                case ResourceType.Series:
                    eTag = await _instanceStore.GetETag(
                                    ResourceType.Series,
                                    uid,
                                    cancellationToken);
                    break;
                case ResourceType.Instance:
                    eTag = await _instanceStore.GetETag(
                                    ResourceType.Instance,
                                    uid,
                                    cancellationToken);
                    break;
                case ResourceType.Frames:
                default:
                    break;
            }

            return eTag;
        }
    }
}
