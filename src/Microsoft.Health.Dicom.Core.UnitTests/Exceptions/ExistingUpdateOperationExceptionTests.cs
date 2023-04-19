// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Exceptions;

public class ExistingUpdateOperationExceptionTests
{
    [Fact]
    public void GivenExistingUpdateOperationExceptionTests_WhenGetMessage_ShouldReturnExpected()
    {
        var guid = Guid.NewGuid();
        var formattedGuid = guid.ToString(OperationId.FormatSpecifier);
        var exception = new ExistingUpdateOperationException(new OperationReference(guid, new Uri("/operation", UriKind.Relative)));
        Assert.Equal($"There is already an active update operation with ID '{formattedGuid}'.", exception.Message);
    }
}
