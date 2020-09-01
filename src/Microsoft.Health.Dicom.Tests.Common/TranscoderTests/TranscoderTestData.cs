// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public class TranscoderTestData
    {
        public string InputDicomFile { get; set; }

        public TranscoderTestMetadata MetaData { get; set; }

        public string ExpectedOutputDicomFile { get; set; }
    }
}
