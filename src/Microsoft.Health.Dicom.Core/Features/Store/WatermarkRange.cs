// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    public class WatermarkRange
    {
        public WatermarkRange(long start, long end)
        {
            Start = start;
            End = end;
        }

        public long Start { get; set; }
        public long End { get; set; }
    }
}
