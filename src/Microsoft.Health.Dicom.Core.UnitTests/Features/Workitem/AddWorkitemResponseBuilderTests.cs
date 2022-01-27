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
        private readonly MockUrlResolver _urlResolver = new MockUrlResolver();
        private readonly DicomDataset _dataset = new DicomDataset();
        private readonly WorkitemResponseBuilder _target;

        public AddWorkitemResponseBuilderTests()
        {
            _target = new WorkitemResponseBuilder(_urlResolver);
        }

        [Fact]
        public void GivenBuildResponse_WhenNoFailure_ThenResponseStatusIsSuccess()
        {
            _dataset.Add(DicomTag.AffectedSOPInstanceUID, DicomUID.Generate().UID);

            _target.AddSuccess(_dataset);

            var response = _target.BuildAddResponse();

            Assert.NotNull(response);
            Assert.Equal(WorkitemResponseStatus.Success, response.Status);
        }

        [Fact]
        public void GivenBuildResponse_WhenNoFailure_ThenResponseUrlIncludesWorkitemInstanceUid()
        {
            var workitemInstanceUid = DicomUID.Generate().UID;
            _dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);

            _target.AddSuccess(_dataset);

            var response = _target.BuildAddResponse();

            Assert.NotNull(response);
            Assert.NotNull(response.Uri);
            Assert.Contains(workitemInstanceUid, response.Uri.ToString());
        }

        [Fact]
        public void GivenBuildResponse_WhenFailure_ThenFailureReasonTagIsAddedToDicomDataset()
        {
            _target.AddFailure(_dataset, (ushort)WorkitemResponseStatus.Failure);

            var response = _target.BuildAddResponse();

            Assert.NotNull(response);
            Assert.NotEmpty(_dataset.GetString(DicomTag.FailureReason));
            Assert.Null(response.Uri);
            Assert.Equal(WorkitemResponseStatus.Failure, response.Status);
        }
    }
}
