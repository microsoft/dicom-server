// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface ITranscoder
    {
        public Task<Stream> TranscodeFileAsync(Stream streams, string requestedTransferSyntax);

        Stream TranscodeFrame(DicomFile dicomFile, int frameIndex, string requestedTransferSyntax);
    }
}
