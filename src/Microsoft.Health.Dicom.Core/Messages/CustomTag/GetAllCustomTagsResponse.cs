// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class GetAllCustomTagsResponse
    {
        public GetAllCustomTagsResponse(IEnumerable<CustomTagEntry> customTagEntries)
        {
            CustomTags = customTagEntries;
        }

        public IEnumerable<CustomTagEntry> CustomTags { get; }
    }
}
