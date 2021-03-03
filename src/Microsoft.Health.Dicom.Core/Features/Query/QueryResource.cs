// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public enum QueryResource
    {
        AllStudies = 0,
        AllSeries = 1,
        StudySeries = 2,
        AllInstances = 3,
        StudyInstances = 4,
        StudySeriesInstances = 5,
    }
}
