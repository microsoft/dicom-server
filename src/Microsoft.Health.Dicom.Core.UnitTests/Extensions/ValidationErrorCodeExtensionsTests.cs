// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class ValidationErrorCodeExtensionsTests
    {
        [Fact]
        public void GivenAnyErrorCode_WhenGetMessage_ThenShouldReturnValue()
        {
            foreach (var value in Enum.GetValues(typeof(ValidationErrorCode)))
            {
                // if not exist, would throw exception
                ((ValidationErrorCode)value).GetMessage();
            }
        }
    }
}
