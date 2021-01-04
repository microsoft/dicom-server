// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.Messages.CustomTag
{
    public class AddCustomTagResponse
    {
        public AddCustomTagResponse(IEnumerable<CustomTagEntry> customTags, string job)
        {
            CustomTags = customTags;
            Job = job;
        }

        public IEnumerable<CustomTagEntry> CustomTags { get; }

        /// <summary>
        /// The Url to view job details.
        /// </summary>
        public string Job { get; }
    }
}
