// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Export;

public class AzureBlobExportOptionsTests
{
    [Theory]
    [InlineData(null, "  ", "mycontainer", "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null, "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, "mycontainer", "%SopInstance%.dcm", "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, null, null, "%Operation%/Errors.log")]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, null, "%SopInstance%.dcm", "")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URIs cannot be used inline.")]
    public void GivenInvalidOptions_WhenValidating_ThenReturnFailures(
        string containerUri,
        string connectionString,
        string containerName,
        string filePattern,
        string errorPattern)
    {
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            ContainerName = containerName,
            ContainerUri = containerUri != null ? new Uri(containerUri, UriKind.Absolute) : null,
            DicomFilePattern = filePattern,
            ErrorLogPattern = errorPattern,
        };

        Assert.Single(options.Validate(null).ToList());
    }
}
