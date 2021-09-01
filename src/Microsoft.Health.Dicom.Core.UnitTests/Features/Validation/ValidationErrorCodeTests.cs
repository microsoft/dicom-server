// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ValidationErrorCodeTests
    {
        [Fact]
        public void GivenErrorCode_WhenConvertToShort_ThenShouldSucceed()
        {
            // we store errorcode in SQL as SMALLINT, so need to verify
            foreach (var value in Enum.GetValues(typeof(ValidationErrorCode)))
            {
                Assert.InRange((int)value, short.MinValue, short.MaxValue);
            }
        }
    }
}
