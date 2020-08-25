// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public class TranscoderTestMetadata
    {
        public string Frame0HashCode { get; set; }

        public string OutputFramesHashCode { get; set; }

        public string OutputSyntaxUid { get; set; }

        public string InputSyntaxUid { get; set; }
    }
}
