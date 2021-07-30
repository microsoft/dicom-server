// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Operations
{
    public class OperationIdTests
    {
        [Fact]
        public void GivenGuid_WhenFormattingAsOperationId_ThenReturnFormattedString()
        {
            Guid guid = Guid.NewGuid();
            Assert.Equal(guid.ToString(OperationId.FormatSpecifier), OperationId.ToString(guid));
        }
    }
}
