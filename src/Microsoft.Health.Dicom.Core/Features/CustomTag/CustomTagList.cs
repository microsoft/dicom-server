// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagList
    {
        private readonly long? _version;
        private readonly IReadOnlyList<CustomTagEntry> _customTags;

        public CustomTagList(IReadOnlyList<CustomTagEntry> customTags)
        {
            EnsureArg.IsNotNull(customTags, nameof(customTags));
            _customTags = customTags;
            if (_customTags.Count != 0)
            {
                _version = _customTags.Max(tag => tag.Version);
            }
        }

        public long? Version { get => _version; }

        public IReadOnlyList<CustomTagEntry> CustomTags { get => _customTags; }
    }
}
