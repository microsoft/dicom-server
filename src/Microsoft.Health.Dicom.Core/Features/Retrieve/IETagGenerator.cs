// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IETagGenerator
    {
        /// <summary>
        /// Get ETag from the list of instances to retrieve.
        /// For study and series resource types, Etag is calculated using the following formula:
        ///     $"{Max(Instance Watermark)}-{Count(Instance)}"
        /// For instance, its watermark is returned as the ETag.
        /// </summary>
        /// <param name="resourceType">Resource Type.</param>
        /// <param name="retrieveInstances">Retrieve Instances.</param>
        /// <returns>ETag.</returns>
        string GetETag(ResourceType resourceType, IEnumerable<VersionedInstanceIdentifier> retrieveInstances);
    }
}
