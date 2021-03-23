// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class DicomTagExtensions
    {
        public static ExtendedQueryTagEntry BuildExtendedQueryTagEntry(this DicomTag tag, string vr = null, string privateCreator = null, ExtendedQueryTagLevel level = ExtendedQueryTagLevel.Series, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagEntry { Path = tag.GetPath(), VR = vr ?? tag.GetDefaultVR()?.Code, PrivateCreator = privateCreator, Level = level, Status = status };
        }

        public static ExtendedQueryTagStoreEntry BuildExtendedQueryTagStoreEntry(this DicomTag tag, int key = 1, string vr = null, string privateCreator = null, ExtendedQueryTagLevel level = ExtendedQueryTagLevel.Series, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Adding)
        {
            return new ExtendedQueryTagStoreEntry(key: key, path: tag.GetPath(), vr: vr ?? tag.GetDefaultVR().Code, privateCreator: privateCreator, level: level, status: status);
        }
    }
}
