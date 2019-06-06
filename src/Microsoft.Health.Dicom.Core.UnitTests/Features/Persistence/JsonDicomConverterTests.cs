// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Dicom.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Persistence
{
    public class JsonDicomConverterTests
    {
        [Fact]
        public static void TestPrivateTagsDeserialization()
        {
            DicomPrivateCreator privateCreator = DicomDictionary.Default.GetPrivateCreator("Testing");
            var privTag1 = new DicomTag(4013, 0x008, privateCreator);
            var privTag2 = new DicomTag(4013, 0x009, privateCreator);

            var ds = new DicomDataset
            {
                { DicomTag.Modality, "CT" },
                new DicomCodeString(privTag1, "TESTA"),
                { privTag2, "TESTB" },
            };

            var json = JsonConvert.SerializeObject(ds, new JsonDicomConverter());
            DicomDataset ds2 = JsonConvert.DeserializeObject<DicomDataset>(json, new JsonDicomConverter());

            Assert.Equal(ds.GetSingleValue<string>(privTag1), ds2.GetSingleValue<string>(privTag1));
            Assert.Equal(ds.GetSingleValue<string>(privTag2), ds2.GetSingleValue<string>(privTag2));
        }
    }
}
