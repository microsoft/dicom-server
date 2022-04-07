// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Store;
public class InstanceFileNameFactory : IInstanceFileNameFactory
{
    public string GetInstanceFileName(DetailedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return GetBaseFileName(instanceIdentifier) + ".dcm";
    }

    private static string GetBaseFileName(DetailedInstanceIdentifier instanceIdentifier)
    {
        switch (instanceIdentifier.MigrationState)
        {
            case MigrationState.NotStarted:
                return $"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}";
            case MigrationState.DataDuplicated:
            case MigrationState.OldDataDeleted:
            default:
                return $"{instanceIdentifier.StudyKey}/{instanceIdentifier.SeriesKey}/{instanceIdentifier.SopKey}_{instanceIdentifier.Version}";
        }
    }

    public string GetInstanceMetadataFileName(DetailedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return GetBaseFileName(instanceIdentifier) + "_metadata.json";
    }
}
