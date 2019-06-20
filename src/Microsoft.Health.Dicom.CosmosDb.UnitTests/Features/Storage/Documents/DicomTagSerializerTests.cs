// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class DicomTagSerializerTests
    {
        [Fact]
        public void GivenPrivateDicomTag_WhenSerialized_IsDeserializedCorrectly()
        {
            var dicomDictionary = new DicomDictionary
            {
                new DicomDictionaryEntry(new DicomTag(0x0011, 0x0010), "TEST CREATOR", "TEST", DicomVM.VM_1, false, DicomVR.LO),
            };

            SerializeAndDeserialize(new DicomTag(0007, 0010));
            SerializeAndDeserialize(new DicomTag(0011, 0010, dicomDictionary.GetPrivateCreator("TEST")));
        }

        [Fact]
        public void GivenValidDicomTag_WhenSerialized_IsDeserializedCorrectly()
        {
            SerializeAndDeserialize(DicomTag.PatientName);
            SerializeAndDeserialize(DicomTag.SOPInstanceStatus);
            SerializeAndDeserialize(DicomTag.SeriesDate);
            SerializeAndDeserialize(DicomTag.SeriesTime);
            SerializeAndDeserialize(DicomTag.AbortReason);
            SerializeAndDeserialize(DicomTag.WindowCenter);
        }

        [Fact]
        public void GivenInvalidDicomTagSerializerParameters_OnSerializeOrDeserialize_ArgumentExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => DicomTagSerializer.Serialize(null));
            Assert.Throws<ArgumentNullException>(() => DicomTagSerializer.Deserialize(null));
            Assert.Throws<ArgumentException>(() => DicomTagSerializer.Deserialize(string.Empty));
        }

        private static void SerializeAndDeserialize(DicomTag dicomTag)
        {
            var serialized = DicomTagSerializer.Serialize(dicomTag);
            DicomTag deserialized = DicomTagSerializer.Deserialize(serialized);

            Assert.Equal(dicomTag, deserialized);
        }
    }
}
