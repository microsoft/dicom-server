// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Health.Dicom.Core.Models.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Export;

public class AzureBlobExportOptionsTests
{
    [Theory]
    [InlineData(null, "  ", "mycontainer")]
    [InlineData(null, "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", "BlobEndpoint=https://unit-test.blob.core.windows.net/;Foo=Bar", null)]
    [InlineData("https://unit-test.blob.core.windows.net/mycontainer", null, "mycontainer")]
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URIs cannot be used inline.")]
    public void GivenInvalidOptions_WhenValidating_ThenReturnFailures(string blobContainerUri, string connectionString, string blobContainerName)
    {
        var options = new AzureBlobExportOptions
        {
            ConnectionString = connectionString,
            BlobContainerName = blobContainerName,
            BlobContainerUri = blobContainerUri != null ? new Uri(blobContainerUri, UriKind.Absolute) : null,
        };

        Assert.Single(options.Validate(null).ToList());
    }
}
