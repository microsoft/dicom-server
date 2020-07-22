// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Messages.Retrieve
{
    public class RetrieveMetadataRequestTests
    {
        private readonly Random _random = new Random();

        [Fact]
        public void GivenRetrieveStudyMetadataRequest_WhenConstructed_ThenResourceTypeAndInstanceUidIsSetCorrectly()
        {
            string studyInstanceUid = Guid.NewGuid().ToString();
            string ifNoneMatch = $"{_random.Next()}-{_random.Next()}";
            var request = new RetrieveMetadataRequest(studyInstanceUid, ifNoneMatch);

            Assert.Equal(ResourceType.Study, request.ResourceType);
            Assert.Equal(studyInstanceUid, request.StudyInstanceUid);
            Assert.Equal(ifNoneMatch, request.IfNoneMatch);
        }

        [Fact]
        public void GivenRetrieveSeriesMetadataRequest_WhenConstructed_ThenResourceTypeAndInstanceUidsAreSetCorrectly()
        {
            string studyInstanceUid = Guid.NewGuid().ToString();
            string seriesInstanceUid = Guid.NewGuid().ToString();
            string ifNoneMatch = $"{_random.Next()}-{_random.Next()}";
            var request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, ifNoneMatch);

            Assert.Equal(ResourceType.Series, request.ResourceType);
            Assert.Equal(studyInstanceUid, request.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, request.SeriesInstanceUid);
            Assert.Equal(ifNoneMatch, request.IfNoneMatch);
        }

        [Fact]
        public void GivenRetrieveSopInstanceMetadataRequest_WhenConstructed_ThenResourceTypeAndInstanceUidsAreSetCorrectly()
        {
            string studyInstanceUid = Guid.NewGuid().ToString();
            string seriesInstanceUid = Guid.NewGuid().ToString();
            string sopInstanceUid = Guid.NewGuid().ToString();
            string ifNoneMatch = $"{_random.Next()}-{_random.Next()}";
            var request = new RetrieveMetadataRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ifNoneMatch);

            Assert.Equal(ResourceType.Instance, request.ResourceType);
            Assert.Equal(studyInstanceUid, request.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, request.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid, request.SopInstanceUid);
            Assert.Equal(ifNoneMatch, request.IfNoneMatch);
        }
    }
}
