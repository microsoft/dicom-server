// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Routing
{
    public interface IUrlResolver
    {
        /// <summary>
        /// Resolves the URI to retrieve a study.
        /// </summary>
        /// <param name="studyInstanceUid">The StudyInstanceUID.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the study can be retrieved.</returns>
        Uri ResolveRetrieveStudyUri(string studyInstanceUid);

        /// <summary>
        /// Resovles the URI to retrieve an instance.
        /// </summary>
        /// <param name="instance">The identifier to the instance.</param>
        /// <returns>An instance of <see cref="Uri"/> pointing to where the instance can be retrieved.</returns>
        Uri ResolveRetrieveInstanceUri(InstanceIdentifier instance);
    }
}
