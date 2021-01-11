// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class GetCustomTagResponse
    {
        public GetCustomTagResponse(CustomTagEntry customTagEntry)
        {
            CustomTag = customTagEntry;
        }

        public GetCustomTagResponse(IEnumerable<CustomTagEntry> customTagEntries)
        {
            CustomTags = customTagEntries;
        }

        /// <summary>
        /// Retrieved custom tag when request specifies exact custom tag path.
        /// </summary>
        public CustomTagEntry CustomTag { get; }

        /// <summary>
        /// All custom tags stored when request is to get all custom tags.
        /// </summary>
        public IEnumerable<CustomTagEntry> CustomTags { get; }
    }
}
