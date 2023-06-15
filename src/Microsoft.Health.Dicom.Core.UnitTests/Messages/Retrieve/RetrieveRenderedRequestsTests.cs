// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve;

public class RetrieveRenderedRequestsTests
{
    [Fact]
    public void VerifyAllFieldsSetCorrectlyForInstance()
    {
        string studyInstanceUid = Guid.NewGuid().ToString();
        string seriesInstanceUid = Guid.NewGuid().ToString();
        string sopInstanceUid = Guid.NewGuid().ToString();
        var request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Instance, 1, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        Assert.Equal(studyInstanceUid, request.StudyInstanceUid);
        Assert.Equal(seriesInstanceUid, request.SeriesInstanceUid);
        Assert.Equal(sopInstanceUid, request.SopInstanceUid);
        Assert.Equal(ResourceType.Instance, request.ResourceType);
        Assert.Equal(0, request.FrameNumber);
        Assert.Equal(75, request.Quality);
    }

    [Fact]
    public void VerifyAllFieldsSetCorrectlyForFrame()
    {
        string studyInstanceUid = Guid.NewGuid().ToString();
        string seriesInstanceUid = Guid.NewGuid().ToString();
        string sopInstanceUid = Guid.NewGuid().ToString();
        var request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 6, 75, new[] { AcceptHeaderHelpers.CreateRenderAcceptHeader() });
        Assert.Equal(studyInstanceUid, request.StudyInstanceUid);
        Assert.Equal(seriesInstanceUid, request.SeriesInstanceUid);
        Assert.Equal(sopInstanceUid, request.SopInstanceUid);
        Assert.Equal(ResourceType.Frames, request.ResourceType);
        Assert.Equal(5, request.FrameNumber);
        Assert.Equal(75, request.Quality);
    }
}
