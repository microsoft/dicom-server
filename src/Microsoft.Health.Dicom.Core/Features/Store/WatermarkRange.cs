// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Watermark range
    /// </summary>
    public class WatermarkRange
    {
        public WatermarkRange(long start, long end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Gets or sets inclusive start watermark.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets inclusive end watermark.
        /// </summary>
        public long End { get; set; }
    }
}
