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

        private readonly IDicomInstanceEntryReader _dicomInstanceEntryReader = Substitute.For<IDicomInstanceEntryReader>();
        private readonly DicomInstanceEntryReaderManager _dicomInstanceEntryReaderManager;

        public DicomInstanceEntryReaderManagerTests()
        {
            _dicomInstanceEntryReader.CanRead(DefaultContentType).Returns(true);

            _dicomInstanceEntryReaderManager = new DicomInstanceEntryReaderManager(
                new[] { _dicomInstanceEntryReader });
        }

        [Fact]
        public void GivenASupportedContentType_WhenFindReaderIsCalled_ThenAnInstanceOfReaderShouldBeReturned()
        {
            Assert.Same(
                _dicomInstanceEntryReader,
                _dicomInstanceEntryReaderManager.FindReader(DefaultContentType));
        }

        [Fact]
        public void GivenANotSupportedContentType_WhenFindReaderIsCalled_ThenNullShouldBeReturned()
        {
            Assert.Null(_dicomInstanceEntryReaderManager.FindReader("unsupported"));
        }
    }
}
