// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class UpdateExtendedQueryTagRequest : IRequest<UpdateExtendedQueryTagResponse>
    {
        public UpdateExtendedQueryTagRequest(string tagPath, UpdateExtendedQueryTagEntry TagEntry)
        {
            TagPath = EnsureArg.IsNotNull(tagPath, nameof(tagPath));
            TagEntry = EnsureArg.IsNotNull(TagEntry, nameof(TagEntry));
        }

        public string TagPath { get; }

        public UpdateExtendedQueryTagEntry TagEntry { get; }
    }
}
