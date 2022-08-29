// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public sealed class WorkitemRequestValidatorExtensionsTests
{
    [Fact]
    public void GivenAddWorkitemRequest_WhenRequestBodyIsNull_ThenBadRequestExceptionIsThrown()
    {
        var target = new AddWorkitemRequest(null, @"application/json", Guid.NewGuid().ToString());

        Assert.Throws<BadRequestException>(() => target.Validate());
    }

    [Fact]
    public void GivenAddWorkitemRequest_WhenWorkitemInstanceUidIsNull_ThenNoExceptionIsThrown()
    {
        var target = new AddWorkitemRequest(new DicomDataset(), @"application/json", null);

        target.Validate();
    }

    [Fact]
    public void GivenAddWorkitemRequest_WhenWorkitemInstanceUidIsLongerThan64Chars_ThenInvalidIdentifierExceptionIsThrown()
    {
        var target = new AddWorkitemRequest(new DicomDataset(), @"application/json", Guid.NewGuid().ToString());

        Assert.Throws<InvalidIdentifierException>(() => target.Validate());
    }

    [Fact]
    public void GivenAddWorkitemRequest_WhenWorkitemInstanceUidIsInvalid_ThenInvalidIdentifierExceptionIsThrown()
    {
        var target = new AddWorkitemRequest(new DicomDataset(), @"application/json", @"12346.8234234.abc.234");

        Assert.Throws<InvalidIdentifierException>(() => target.Validate());
    }

    [Fact]
    public void GivenAddWorkitemRequest_WhenWorkitemInstanceUidIsValid_ThenNoExceptionIsThrown()
    {
        var target = new AddWorkitemRequest(new DicomDataset(), @"application/json", @"12346.8234234.234");

        target.Validate();
    }
}
