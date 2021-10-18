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
        public UpdateExtendedQueryTagRequest(string tagPath, UpdateExtendedQueryTagEntry newValue)
        {
            TagPath = EnsureArg.IsNotNull(tagPath, nameof(tagPath));
            NewValue = EnsureArg.IsNotNull(newValue, nameof(newValue));
        }

        public string TagPath { get; }

        public UpdateExtendedQueryTagEntry NewValue { get; }
    }
}
