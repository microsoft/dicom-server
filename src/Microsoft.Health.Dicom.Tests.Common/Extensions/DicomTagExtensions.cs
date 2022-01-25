// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;

namespace Microsoft.Health.Dicom.Tests.Common.Extensions
{
    public static class DicomTagExtensions
    {
        public static AddExtendedQueryTagEntry BuildAddExtendedQueryTagEntry(this DicomTag tag, string vr = null, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Series)
        {
            return new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = vr ?? tag.GetDefaultVR()?.Code, PrivateCreator = privateCreator, Level = level };
        }

        public static GetExtendedQueryTagEntry BuildGetExtendedQueryTagEntry(this DicomTag tag, string vr = null, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Series, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new GetExtendedQueryTagEntry { Path = tag.GetPath(), VR = vr ?? tag.GetDefaultVR()?.Code, PrivateCreator = privateCreator, Level = level, Status = status };
        }

        public static ExtendedQueryTagStoreEntry BuildExtendedQueryTagStoreEntry(this DicomTag tag, int key = 1, string vr = null, string privateCreator = null, QueryTagLevel level = QueryTagLevel.Series, ExtendedQueryTagStatus status = ExtendedQueryTagStatus.Ready)
        {
            return new ExtendedQueryTagStoreEntry(key: key, path: tag.GetPath(), vr: vr ?? tag.GetDefaultVR().Code, privateCreator: privateCreator, level: level, status: status, queryStatus: QueryStatus.Enabled, errorCount: 0);
        }

        public static WorkitemQueryTagStoreEntry BuildWorkitemQueryTagStoreEntry(string path, int key, string vr)
        {
            var dicomTagParser = new DicomTagParser();
            var entry = new WorkitemQueryTagStoreEntry(key, path, vr);
            if (dicomTagParser.TryParse(path, out var dicomTags))
            {
                entry.PathTags = Array.AsReadOnly(dicomTags);
            }

            return entry;
        }
    }
}
