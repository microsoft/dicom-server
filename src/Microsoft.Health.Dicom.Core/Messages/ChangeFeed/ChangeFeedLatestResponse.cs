// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Messages.ChangeFeed
{
    public class ChangeFeedLatestResponse
    {
        public ChangeFeedLatestResponse(ChangeFeedEntry entry)
        {
            Entry = entry;
        }

        public ChangeFeedEntry Entry { get; }
    }
}
