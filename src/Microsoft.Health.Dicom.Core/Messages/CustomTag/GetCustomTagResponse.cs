// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class GetCustomTagResponse
    {
        public GetCustomTagResponse(CustomTagEntry customTagEntry)
        {
            CustomTag = customTagEntry;
        }

        public CustomTagEntry CustomTag { get; }
    }
}
