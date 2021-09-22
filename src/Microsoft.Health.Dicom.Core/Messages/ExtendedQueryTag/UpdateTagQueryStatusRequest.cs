// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class UpdateTagQueryStatusRequest : IRequest<UpdateTagQueryStatusResponse>
    {
        public UpdateTagQueryStatusRequest(string tagPath, QueryStatus queryStatus)
        {
            TagPath = EnsureArg.IsNotNull(tagPath, nameof(tagPath));
            QueryStatus = EnsureArg.EnumIsDefined(queryStatus, nameof(queryStatus));
        }

        public string TagPath { get; }
        public QueryStatus QueryStatus { get; }
    }
}
