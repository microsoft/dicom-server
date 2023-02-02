// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query;

public class ResultsTests
{

    [Fact]
    public void GivenStudyResultsWithNullValues_DicomDatasetConversion_Works()
    {
        var StudyResult = new StudyResult()
        {
            StudyInstanceUid = "1.2.3",
            NumberofStudyRelatedInstances = 1,
            ModalitiesInStudy = new string[] { },
        };
        Assert.True(StudyResult.DicomDataset.Count() == 2);
    }

    [Fact]
    public void GivenSeriesResultsWithNullValues_DicomDatasetConversion_Works()
    {
        var StudyResult = new SeriesResult()
        {
            StudyInstanceUid = "1.2.3",
            SeriesInstanceUid = "1.2.3.4",
            NumberOfSeriesRelatedInstances = 1,
        };
        Assert.True(StudyResult.DicomDataset.Count() == 3);
    }
}
