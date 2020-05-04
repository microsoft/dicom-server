// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IDicomFrameHandler
    {
        Task<Stream[]> GetFramesResourceAsync(Stream stream, IEnumerable<int> frames, bool originalTransferSyntaxRequested, string requestedRepresentation);
    }
}
