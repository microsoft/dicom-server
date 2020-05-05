// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Store.Entries;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store.Entries
{
    public class DicomInstanceEntryReaderManagerTests
    {
        private const string DefaultContentType = "test-content";

        private readonly IInstanceEntryReader _instanceEntryReader = Substitute.For<IInstanceEntryReader>();
        private readonly InstanceEntryReaderManager _instanceEntryReaderManager;

        public DicomInstanceEntryReaderManagerTests()
        {
            _instanceEntryReader.CanRead(DefaultContentType).Returns(true);

            _instanceEntryReaderManager = new InstanceEntryReaderManager(
                new[] { _instanceEntryReader });
        }

        [Fact]
        public void GivenASupportedContentType_WhenFindReaderIsCalled_ThenAnInstanceOfReaderShouldBeReturned()
        {
            Assert.Same(
                _instanceEntryReader,
                _instanceEntryReaderManager.FindReader(DefaultContentType));
        }

        [Fact]
        public void GivenANotSupportedContentType_WhenFindReaderIsCalled_ThenNullShouldBeReturned()
        {
            Assert.Null(_instanceEntryReaderManager.FindReader("unsupported"));
        }
    }
}
