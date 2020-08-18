// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class TranscoderTestMetadata
    {
        public string OutputFramesHashCode { get; set; }

        public string OutputSyntaxUid { get; set; }

        public string InputSyntaxUid { get; set; }

        public DicomTransferSyntax GetInputSyntax()
        {
            return DicomTransferSyntax.Parse(InputSyntaxUid);
        }

        public DicomTransferSyntax GetOutputSyntax()
        {
            return DicomTransferSyntax.Parse(OutputSyntaxUid);
        }
    }
}
