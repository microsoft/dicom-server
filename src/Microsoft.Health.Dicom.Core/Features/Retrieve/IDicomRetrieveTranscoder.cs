// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public interface IDicomRetrieveTranscoder
    {
        (bool, Stream[]) TranscodeDicomFiles(Stream[] streams, string requestedTransferSyntax);

        Stream TranscodeDicomFrame(DicomFile dicomFile, int frame, string requestedTransferSyntax);
    }
}
