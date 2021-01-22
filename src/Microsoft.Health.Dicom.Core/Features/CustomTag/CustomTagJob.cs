// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Level of a custom tag.
    /// </summary>
    public class CustomTagJob
    {
        public CustomTagJob()
        {
        }

        public CustomTagJob(long key, CustomTagJobType type, long? completedWatermark, long maxWatermark, DateTime? heartBeatTimeStamp, CustomTagJobStatus status)
        {
            Key = key;
            Type = type;
            CompletedWatermark = completedWatermark;
            MaxWatermark = maxWatermark;
            HeartBeatTimeStamp = heartBeatTimeStamp;
            Status = status;
        }

        public long Key { get; set; }

        public CustomTagJobType Type { get; set; }

        public long? CompletedWatermark { get; set; }

        public long MaxWatermark { get; set; }

        public DateTime? HeartBeatTimeStamp { get; set; }

        public CustomTagJobStatus Status { get; set; }
    }
}
