// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;
public class DicomFileNameWithUid : IDicomFileNameBuilder
{
    public string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}.dcm";
    }

    public string GetMetadataFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}_metadata.json";
    }
}
