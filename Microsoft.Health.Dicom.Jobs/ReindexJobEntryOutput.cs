// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Jobs
{
    // TODO: should have a way to return processed result
    public class ReindexJobEntryOutput
    {
        public long NextMaxWatermark { get; set; }
    }
}
