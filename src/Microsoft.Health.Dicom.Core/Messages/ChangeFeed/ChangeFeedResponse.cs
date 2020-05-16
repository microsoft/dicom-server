// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed
{
    public class ChangeFeedResponse
    {
        public ChangeFeedResponse(IReadOnlyCollection<ChangeFeedEntry> entries)
        {
            EnsureArg.IsNotNull(entries, nameof(entries));

            Entries = entries;
        }

        public IReadOnlyCollection<ChangeFeedEntry> Entries { get; }
    }
}
