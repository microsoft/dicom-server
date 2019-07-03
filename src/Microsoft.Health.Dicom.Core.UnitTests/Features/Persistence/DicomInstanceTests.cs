// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Persistence
{
    public class DicomInstanceTests
    {
        [Fact]
        public void GivenDicomInstanceSeriesStudy_WhenConstructedWithInvalidParameters_ArgumentExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new DicomStudy(null));
            Assert.Throws<ArgumentException>(() => new DicomStudy(string.Empty));
            Assert.Throws<ArgumentException>(() => new DicomStudy("#"));
            Assert.Throws<ArgumentException>(() => new DicomStudy(new string('a', 65)));

            Assert.Throws<ArgumentNullException>(() => new DicomSeries(null, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomSeries(string.Empty, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomSeries("#", Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomSeries(new string('a', 65), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentNullException>(() => new DicomSeries(Guid.NewGuid().ToString(), null));
            Assert.Throws<ArgumentException>(() => new DicomSeries(Guid.NewGuid().ToString(), string.Empty));
            Assert.Throws<ArgumentException>(() => new DicomSeries(Guid.NewGuid().ToString(), "#"));
            Assert.Throws<ArgumentException>(() => new DicomSeries(Guid.NewGuid().ToString(), new string('a', 65)));
            Assert.Throws<ArgumentException>(() => new DicomSeries("aaa", "aaa"));

            Assert.Throws<ArgumentNullException>(() => new DicomInstance(null, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance(string.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance("#", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance(new string('a', 65), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentNullException>(() => new DicomInstance(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), string.Empty, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), "#", Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), new string('a', 65), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentNullException>(() => new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), string.Empty));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "#"));
            Assert.Throws<ArgumentException>(() => new DicomInstance(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new string('a', 65)));
            Assert.Throws<ArgumentException>(() => new DicomInstance("aaa", "aaa", "bbb"));
            Assert.Throws<ArgumentException>(() => new DicomInstance("aaa", "bbb", "aaa"));
            Assert.Throws<ArgumentException>(() => new DicomInstance("bbb", "aaa", "aaa"));
        }

        [Fact]
        public void GivenDicomInstanceSeriesStudy_WhenCompared_CorrectEqualsResponseReturned()
        {
            Assert.Equal("aaa", new DicomStudy("aaa").StudyInstanceUID);
            Assert.Equal("aaa".GetHashCode(), new DicomStudy("aaa").GetHashCode());
            Assert.Equal(new DicomStudy("aaa"), new DicomStudy("aaa"));
            Assert.NotEqual(new DicomStudy("aAa"), new DicomStudy("aaa"));

            Assert.Equal("aaa", new DicomSeries("aaa", "bbb").StudyInstanceUID);
            Assert.Equal("bbb", new DicomSeries("aaa", "bbb").SeriesInstanceUID);
            Assert.Equal("aaabbb".GetHashCode(), new DicomSeries("aaa", "bbb").GetHashCode());
            Assert.Equal(new DicomSeries("aaa", "bbb"), new DicomSeries("aaa", "bbb"));
            Assert.NotEqual(new DicomSeries("aAa", "bbb"), new DicomSeries("aaa", "bbb"));
            Assert.NotEqual(new DicomSeries("aaa", "bBb"), new DicomSeries("aaa", "bbb"));

            Assert.Equal("aaa", new DicomInstance("aaa", "bbb", "ccc").StudyInstanceUID);
            Assert.Equal("bbb", new DicomInstance("aaa", "bbb", "ccc").SeriesInstanceUID);
            Assert.Equal("ccc", new DicomInstance("aaa", "bbb", "ccc").SopInstanceUID);
            Assert.Equal("aaabbbccc".GetHashCode(), new DicomInstance("aaa", "bbb", "ccc").GetHashCode());
            Assert.Equal(new DicomInstance("aaa", "bbb", "ccc"), new DicomInstance("aaa", "bbb", "ccc"));
            Assert.NotEqual(new DicomInstance("aAa", "bbb", "ccc"), new DicomInstance("aaa", "bbb", "ccc"));
            Assert.NotEqual(new DicomInstance("aaa", "bBb", "ccc"), new DicomInstance("aaa", "bbb", "ccc"));
            Assert.NotEqual(new DicomInstance("aaa", "bbb", "cCc"), new DicomInstance("aaa", "bbb", "ccc"));
        }
    }
}
