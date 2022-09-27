// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve;

public class RetrieveMetadataResponse
{
    public RetrieveMetadataResponse(IAsyncEnumerable<DicomDataset> responseMetadata, bool isCacheValid = false, string eTag = null)
    {
        EnsureArg.IsNotNull(responseMetadata, nameof(responseMetadata));
        ResponseMetadata = responseMetadata;
        IsCacheValid = isCacheValid;
        ETag = eTag;
    }

    public IAsyncEnumerable<DicomDataset> ResponseMetadata { get; }

    public bool IsCacheValid { get; }

    public string ETag { get; }
}
