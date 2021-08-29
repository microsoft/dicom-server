// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    /// <summary>
    /// Represents a utility for creating URLs for the DICOM service.
    /// </summary>
    public interface IUrlResolver
    {
        /// <summary>
        /// Resolves the URI for retrieving the status of an operation.
        /// </summary>
        /// <param name="operationId">The unique ID for a long-running operation.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the status can be retrieved.</returns>
        Uri ResolveOperationStatusUri(Guid operationId);

        /// <summary>
        /// Resolves the URI for retrieving an extended query tag.
        /// </summary>
        /// <param name="tagPath">The extended query tag path.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the extended query tag can be retrieved.</returns>
        Uri ResolveQueryTagUri(string tagPath);

        /// <summary>
        /// Resolves the URI to retrieve a study.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the study can be retrieved.</returns>
        Uri ResolveRetrieveStudyUri(string studyInstanceUid);

        /// <summary>
        /// Resovles the URI to retrieve an instance.
        /// </summary>
        /// <param name="instanceIdentifier">The identifier to the instance.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the instance can be retrieved.</returns>
        Uri ResolveRetrieveInstanceUri(InstanceIdentifier instanceIdentifier);
    }
}
