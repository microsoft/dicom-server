// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag
{
    public class GetAllExtendedQueryTagsResponse
    {
        public GetAllExtendedQueryTagsResponse(IEnumerable<GetExtendedQueryTagEntry> extendedQueryTagEntries)
        {
            ExtendedQueryTags = extendedQueryTagEntries;
        }

        public IEnumerable<GetExtendedQueryTagEntry> ExtendedQueryTags { get; }
    }
}
