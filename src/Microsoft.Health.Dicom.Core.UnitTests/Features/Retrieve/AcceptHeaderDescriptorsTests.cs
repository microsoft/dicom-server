// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class AcceptHeaderDescriptorsTests
    {
        [Fact]
        public void GivenDescriptorsIsNotNull_WhenConstructAcceptHeaderDescriptors_ThenShouldSucceed()
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
            AcceptHeaderDescriptor descriptor = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
            AcceptHeaderDescriptors descriptors = new AcceptHeaderDescriptors(descriptor);
            Assert.Single(descriptors.Descriptors);
            Assert.Same(descriptor, descriptors.Descriptors.First());
        }

        [Fact]
        public void GivenAcceptHeaders_WhenSeveralMatchAndOthersNot_ThenShouldReturnFirstMatched()
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
            AcceptHeaderDescriptor matchDescriptor1 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
            AcceptHeaderDescriptor matchDescriptor2 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: true);
            AcceptHeaderDescriptor notMatchDescriptor = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
            AcceptHeaderDescriptors acceptHeaderDescriptors = new AcceptHeaderDescriptors(matchDescriptor1, matchDescriptor2, notMatchDescriptor);
            AcceptHeaderDescriptor result;
            string transferSyntax;
            Assert.True(acceptHeaderDescriptors.TryGetMatchedDescriptor(acceptHeader, out result, out transferSyntax));
            Assert.Same(result, matchDescriptor1);

            // Actual transferSyntax should be from matchDescriptor1
            string expectedTransferSyntax;
            matchDescriptor1.IsAcceptable(acceptHeader, out expectedTransferSyntax);
            Assert.Equal(transferSyntax, expectedTransferSyntax);
        }

        [Fact]
        public void GivenAcceptHeaders_WhenNoMatch_ThenShouldReturnFalse()
        {
            AcceptHeader acceptHeader = AcceptHeaderHelpers.CreateAcceptHeader();
            AcceptHeaderDescriptor notMatchDescriptor1 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
            AcceptHeaderDescriptor notMatchDescriptor2 = AcceptHeaderDescriptorHelpers.CreateAcceptHeaderDescriptor(acceptHeader, match: false);
            AcceptHeaderDescriptors acceptHeaderDescriptors = new AcceptHeaderDescriptors(notMatchDescriptor1, notMatchDescriptor2);
            AcceptHeaderDescriptor result;
            string transferSyntax;
            Assert.False(acceptHeaderDescriptors.TryGetMatchedDescriptor(acceptHeader, out result, out transferSyntax));
        }
    }
}
