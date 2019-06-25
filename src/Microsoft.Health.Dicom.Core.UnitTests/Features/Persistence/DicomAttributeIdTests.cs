// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Persistence
{
    public class DicomAttributeIdTests
    {
        [Fact]
        public void GivenDicomAttributeId_WhenConstructingWithInvalidParameters_ArgumentExceptionThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new DicomAttributeId((DicomTag[])null));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId(Array.Empty<DicomTag>()));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId(DicomTag.RightImageSequence));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId(DicomTag.RightImageSequence, DicomTag.ROIContourSequence));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId(DicomTag.StudyDate, DicomTag.RightLensSequence));

            Assert.Throws<ArgumentNullException>(() => new DicomAttributeId((string)null));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId(string.Empty));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId("INVALID"));
            Assert.Throws<ArgumentException>(() => new DicomAttributeId("INVALID.INVALID"));
        }

        [Fact]
        public void GivenValidDicomAttributeId_WhenSerialized_IsDeserializedCorrectly()
        {
            SerializeAndDeserialize(new DicomAttributeId(DicomTag.StudyDate));
            SerializeAndDeserialize(new DicomAttributeId(DicomTag.RelatedSeriesSequence, DicomTag.RegistrationSequence, DicomTag.RegisteredLocalizerUnits));
            SerializeAndDeserialize(new DicomAttributeId(DicomTag.RegionPixelShiftSequence, DicomTag.NumberOfVerticalPixels));
            SerializeAndDeserialize(new DicomAttributeId("0020000D"));
            SerializeAndDeserialize(new DicomAttributeId("00101002.00100020"));
            SerializeAndDeserialize(new DicomAttributeId("00101002.00100024.00400032"));
            SerializeAndDeserialize(new DicomAttributeId("StudyInstanceUID"));
            SerializeAndDeserialize(new DicomAttributeId("OtherPatientIDsSequence.PatientID"));
            SerializeAndDeserialize(new DicomAttributeId("OtherPatientIDsSequence.IssuerOfPatientIDQualifiersSequence.UniversalEntityID"));
        }

        [Fact]
        public void GivenValidDicomAttributeId_WhenCompared_IsComparedCorrectly()
        {
            Assert.Equal(new DicomAttributeId(DicomTag.StudyInstanceUID), new DicomAttributeId("StudyInstanceUID"));
            Assert.Equal(new DicomAttributeId(DicomTag.OtherPatientIDsSequence, DicomTag.PatientID), new DicomAttributeId("OtherPatientIDsSequence.PatientID"));

            Assert.Equal(
                new DicomAttributeId(DicomTag.OtherPatientIDsSequence, DicomTag.IssuerOfPatientIDQualifiersSequence, DicomTag.UniversalEntityID),
                new DicomAttributeId("OtherPatientIDsSequence.IssuerOfPatientIDQualifiersSequence.UniversalEntityID"));
        }

        private void SerializeAndDeserialize(DicomAttributeId dicomAttributeId)
        {
            var json = JsonConvert.SerializeObject(dicomAttributeId);
            var deserialized = JsonConvert.DeserializeObject<DicomAttributeId>(json);

            Assert.Equal(dicomAttributeId, deserialized);
        }
    }
}
