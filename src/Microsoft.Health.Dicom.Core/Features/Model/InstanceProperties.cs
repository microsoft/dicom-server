// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class InstanceProperties
{
    public string TransferSyntaxUid { get; set; }

    public bool HasFrameMetadata { get; set; }

    public long? OriginalVersion { get; set; }

    public long? NewVersion { get; set; }

    public string BlobFilePath { get; set; }

    public string BlobStoreOperationETag { get; set; }

    public long? StreamLength { get; set; }
}
