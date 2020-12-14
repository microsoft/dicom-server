// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.CustomTag
{
    public static class CustomTagTestHelper
    {
        public static CustomTagEntry CreateCustomTagEntry(long tagId = 1, string path = "00010002", string vr = DicomVRCode.SS, CustomTagLevel level = CustomTagLevel.Instance, long version = 1L, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagEntry() { Level = level, Path = path, Status = status, TagId = tagId, Version = version, VR = vr };
        }
    }
}
