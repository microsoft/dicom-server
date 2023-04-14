// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Blob.Features.Storage;
public interface IDicomFileNameBuilder
{
    string GetInstanceFileName(long version);

    string GetMetadataFileName(long version);

    string GetInstanceFramesRangeFileName(long version);

    string GetInstanceFramesRangeFileNameWithSpace(long version);
}
