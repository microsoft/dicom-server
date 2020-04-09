// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public sealed class DicomQueryResourceResponse
    {
        public DicomQueryResourceResponse(IAsyncEnumerable<DicomDataset> responseDataset = null)
        {
            ResponseDataset = responseDataset ?? AsyncEnumerable.Empty<DicomDataset>();
            IsEmpty = responseDataset == null ? true : false;
        }

        public bool IsEmpty { get; }

        public IAsyncEnumerable<DicomDataset> ResponseDataset { get; }
    }
}
