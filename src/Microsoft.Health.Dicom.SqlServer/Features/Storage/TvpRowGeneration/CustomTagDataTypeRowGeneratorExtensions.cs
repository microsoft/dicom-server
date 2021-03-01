// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;

namespace Microsoft.Health.Dicom.SqlServer.Features.Storage.TvpRowGeneration
{
    internal static class CustomTagDataTypeRowGeneratorExtensions
    {
        public static Dictionary<long, (DicomTag, CustomTagLevel)> DetermineKeysAndTagsForDataType(IReadOnlyList<CustomTagStoreEntry> storedEntries, CustomTagDataType dataType)
        {
            DicomTagParser dicomTagParser = new DicomTagParser();
            Dictionary<long, (DicomTag, CustomTagLevel)> keysAndTagsForDataType = new Dictionary<long, (DicomTag, CustomTagLevel)>();
            foreach (CustomTagStoreEntry storedEntry in storedEntries)
            {
                if (CustomTagLimit.CustomTagVRAndDataTypeMapping[storedEntry.VR] == dataType)
                {
                    DicomTag[] tags;
                    if (dicomTagParser.TryParse(storedEntry.Path, out tags))
                    {
                        keysAndTagsForDataType.Add(storedEntry.Key, (tags[0], storedEntry.Level));
                    }
                }
            }

            return keysAndTagsForDataType;
        }
    }
}
