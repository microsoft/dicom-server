// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class AddWorkitemRequestHandlerTests
    {
        private readonly IWorkitemSerializer _workitemSerializer = Substitute.For<IWorkitemSerializer>();
        private readonly IWorkitemService _workitemService = Substitute.For<IWorkitemService>();
        private readonly AddWorkitemRequestHandler _target;

        public AddWorkitemRequestHandlerTests()
        {
            _target = new AddWorkitemRequestHandler(new DisabledAuthorizationService<DataActions>(), _workitemSerializer, _workitemService);
        }

        [Fact]
        public async Task GivenSupportedContentType_WhenHandled_ThenCorrectStoreResponseShouldBeReturned()
        {
            var workitemInstanceUid = string.Empty;
            var request = new AddWorkitemRequest(Stream.Null, @"application/json", workitemInstanceUid);

            var response = new AddWorkitemResponse(WorkitemResponseStatus.Success, new Uri(@"https://www.microsoft.com"));

            _workitemService
                .ProcessAsync(Arg.Any<DicomDataset>(), workitemInstanceUid, CancellationToken.None)
                .Returns(response);

            Assert.Same(response, await _target.Handle(request, CancellationToken.None));
        }
    }
}
