// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public enum QueryResource
    {
        // The order of these enum values is important as it allows for a simple way to determine what the minimum resource level requested is.
        // It is currently used to identify that a given query parameter is not supported for a resource level.
        // For example, Study Series is looking for series and therefore an instance tag would be invalid.
        // 0 -> Studies, 1 & 2 -> Series, >= 3 -> Instances.
        AllStudies,
        AllSeries,
        StudySeries,
        AllInstances,
        StudyInstances,
        StudySeriesInstances,
    }
}
