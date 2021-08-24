// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class AcknowledgeTagErrorRequest : IRequest<AcknowledgeTagErrorResponse>
    {
        public AcknowledgeTagErrorRequest(string extendedQueryTagPath, InstanceIdentifier instanceIdentifier)
        {
            ExtendedQueryTagPath = extendedQueryTagPath;
            InstanceIdentifier = EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        }

        /// <summary>
        /// Path for the extended query tag that is requested.
        /// </summary>
        public string ExtendedQueryTagPath { get; }
        public InstanceIdentifier InstanceIdentifier { get; }
    }
}
