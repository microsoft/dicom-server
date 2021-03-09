// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.CustomTag;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class DicomTagExtensions
    {
        public static CustomTagEntry BuildCustomTagEntry(this DicomTag tag, CustomTagLevel level = CustomTagLevel.Series, CustomTagStatus status = CustomTagStatus.Added)
        {
            return new CustomTagEntry(tag.GetPath(), tag.GetDefaultVR()?.Code, level, status);
        }

        public static IndexTag BuildIndexTag(this DicomTag tag, DicomVR vr = null, CustomTagLevel level = CustomTagLevel.Series, bool isCustomTag = true)
        {
            return new IndexTag(tag, vr ?? tag.GetDefaultVR(), level, isCustomTag: isCustomTag);
        }

        public static CustomTagStoreEntry BuildCustomTagStoreEntry(this DicomTag tag, int key = 1, CustomTagLevel level = CustomTagLevel.Series, CustomTagStatus status = CustomTagStatus.Reindexing)
        {
            return new CustomTagStoreEntry(key: key, path: tag.GetPath(), vr: tag.DictionaryEntry.ValueRepresentations[0].Code, level: level, status: status);
        }
    }
}
