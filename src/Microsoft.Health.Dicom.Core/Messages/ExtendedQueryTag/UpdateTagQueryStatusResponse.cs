// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class UpdateExtendedQueryTagResponse
    {
        public UpdateExtendedQueryTagResponse(GetExtendedQueryTagEntry tagEntry)
        {
            TagEntry = EnsureArg.IsNotNull(tagEntry, nameof(tagEntry));
        }

        public GetExtendedQueryTagEntry TagEntry { get; }
    }
}
