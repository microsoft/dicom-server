// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Storage;

public class DicomPrefixedFileNameBuilderTests
{
    private readonly DicomPrefixedFileNameBuilder _builder = new DicomPrefixedFileNameBuilder();
    private static readonly Regex ExpectedFileFormat = new Regex(@"(\w{3})_([^_]+).dcm", RegexOptions.Compiled);
    private static readonly Regex ExpectedMetadataFormat = new Regex(@"(\w{3})_([^_]+)_metadata.json", RegexOptions.Compiled);
    private static readonly Regex ExpectedWorkItemFormat = new Regex(@"(\w{3})_([^_]+)_workitem.json", RegexOptions.Compiled);

    [Fact]
    public void GivenIdentifier_GetFileNames_ShouldReturnExpectedValues()
    {
        var random = new Random(nameof(GivenIdentifier_GetFileNames_ShouldReturnExpectedValues).GetHashCode());
        var instanceIdentifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        Assert.Matches(ExpectedFileFormat, _builder.GetInstanceFileName(instanceIdentifier));
        Assert.Matches(ExpectedMetadataFormat, _builder.GetMetadataFileName(instanceIdentifier));
        Assert.Matches(ExpectedWorkItemFormat, _builder.GetWorkItemFileName(random.NextInt64()));
    }
}
