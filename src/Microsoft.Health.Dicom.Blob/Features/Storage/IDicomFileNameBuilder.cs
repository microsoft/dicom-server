// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

public interface IDicomFileNameBuilder
{
    string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier);

    string GetMetadataFileName(VersionedInstanceIdentifier instanceIdentifier);

    string GetWorkItemFileName(long version);
}
