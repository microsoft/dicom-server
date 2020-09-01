// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.Json;
using Xunit.Abstractions;

namespace Microsoft.Health.Dicom.Tests.Common.TranscoderTests
{
    public class TranscoderTestData : IXunitSerializable
    {
        public string InputDicomFile { get; set; }

        public TranscoderTestMetadata MetaData { get; set; }

        public string ExpectedOutputDicomFile { get; set; }

        public void Deserialize(IXunitSerializationInfo info)
        {
            TranscoderTestData data = JsonSerializer.Deserialize<TranscoderTestData>(info.GetValue<string>(nameof(TranscoderTestData)));
            InputDicomFile = data.InputDicomFile;
            MetaData = data.MetaData;
            ExpectedOutputDicomFile = data.ExpectedOutputDicomFile;
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(TranscoderTestData), JsonSerializer.Serialize(this));
        }
    }
}
