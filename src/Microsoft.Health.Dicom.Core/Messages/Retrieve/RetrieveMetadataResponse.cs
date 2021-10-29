// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using FellowOakDicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class RetrieveMetadataResponse
    {
        public RetrieveMetadataResponse(IEnumerable<DicomDataset> responseMetadata, bool isCacheValid = false, string eTag = null)
        {
            EnsureArg.IsNotNull(responseMetadata, nameof(responseMetadata));
            ResponseMetadata = responseMetadata;
            IsCacheValid = isCacheValid;
            ETag = eTag;
        }

        public IEnumerable<DicomDataset> ResponseMetadata { get; }

        public bool IsCacheValid { get; }

        public string ETag { get; }
    }
}
