// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Jobs
{
    public class ReindexJobEntryInput
    {
        public string JobId { get; set; }

        public long MaxWatermark { get; set; }


        public override string ToString()
        {
            return $"JobId:{JobId}, MaxWatermark: {MaxWatermark}, TopN: {TopN}";
        }
    }
}
