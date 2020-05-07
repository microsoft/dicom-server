// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveMetadataResponse
    {
        public RetrieveMetadataResponse(IEnumerable<DicomDataset> responseMetadata)
        {
            EnsureArg.IsNotNull(responseMetadata, nameof(responseMetadata));
            ResponseMetadata = responseMetadata;
        }

        public IEnumerable<DicomDataset> ResponseMetadata { get; }
    }
}
