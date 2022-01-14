// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class AddWorkitemResponseBuilderTests
    {
        [Fact]
        public void GivenBuildResponse_WhenNoFailure_ThenResponseStatusIsSuccess()
        {
            var dataset = new DicomDataset();
            var urlResolver = new MockUrlResolver();
            var target = new AddWorkitemResponseBuilder(urlResolver);

            dataset.Add(DicomTag.RequestedSOPInstanceUID, DicomUID.Generate().UID);

            target.AddSuccess(dataset);

            var response = target.BuildResponse();

            Assert.NotNull(response);
            Assert.Equal(WorkitemResponseStatus.Success, response.Status);
        }

        [Fact]
        public void GivenBuildResponse_WhenNoFailure_ThenResponseUrlIncludesWorkitemInstanceUid()
        {
            var dataset = new DicomDataset();
            var urlResolver = new MockUrlResolver();
            var target = new AddWorkitemResponseBuilder(urlResolver);

            var workitemInstanceUid = DicomUID.Generate().UID;
            dataset.Add(DicomTag.RequestedSOPInstanceUID, workitemInstanceUid);

            target.AddSuccess(dataset);

            var response = target.BuildResponse();

            Assert.NotNull(response);
            Assert.NotNull(response.Url);
            Assert.Contains(workitemInstanceUid, response.Url.ToString());
        }

        [Fact]
        public void GivenBuildResponse_WhenFailure_ThenFailureReasonTagIsAddedToDicomDataset()
        {
            var dataset = new DicomDataset();
            var urlResolver = new MockUrlResolver();
            var target = new AddWorkitemResponseBuilder(urlResolver);

            target.AddFailure(dataset, (ushort)WorkitemResponseStatus.Failure);

            var response = target.BuildResponse();

            Assert.NotNull(response);
            Assert.NotEmpty(dataset.GetString(DicomTag.FailureReason));
            Assert.Null(response.Url);
            Assert.Equal(WorkitemResponseStatus.Failure, response.Status);
        }
    }
}
