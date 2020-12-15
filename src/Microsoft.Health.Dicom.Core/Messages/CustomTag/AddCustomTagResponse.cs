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
        private readonly IEnumerable<CustomTagEntry> _customTags;
        private readonly string _job;

        public AddCustomTagResponse(IEnumerable<CustomTagEntry> customTags, string job)
        {
            _customTags = customTags;
            _job = job;
        }

        public IEnumerable<CustomTagEntry> CustomTags { get => _customTags; }

        /// <summary>
        /// The Url to view job details.
        /// </summary>
        public string Job { get => _job; }
    }
}
