// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;
public class WatermarkAsInstanceNameTests
{
    private readonly WatermarkAsInstanceName _watearmarkAsInstanceName;
    public WatermarkAsInstanceNameTests()
    {
        _watearmarkAsInstanceName = new WatermarkAsInstanceName();
    }

    [Fact]
    public void GivenVersionedInstanceIdentifier_GetNames_ShouldReturnExpectedValues()
    {
        VersionedInstanceIdentifier versionedId = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        Assert.Equal($"{versionedId.Version}.dcm", _watearmarkAsInstanceName.GetInstanceFileName(versionedId));
        Assert.Equal($"{versionedId.Version}_metadata.json", _watearmarkAsInstanceName.GetInstanceMetadataFileName(versionedId));
    }

}
