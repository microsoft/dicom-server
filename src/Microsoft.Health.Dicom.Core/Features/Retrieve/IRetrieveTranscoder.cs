// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IRetrieveTranscoder
    {
        (bool, Stream[]) TranscodeFiles(Stream[] streams, string requestedTransferSyntax);

        Stream TranscodeFrame(DicomFile dicomFile, int frame, string requestedTransferSyntax);
    }
}
