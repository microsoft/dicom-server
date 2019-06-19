// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class QueryInstanceTests
    {
        [Fact]
        public void GivenInvalidParameters_WhenCreatingQueryInstance_ExceptionsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryInstance(null, new Dictionary<DicomTag, object>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance(string.Empty, new Dictionary<DicomTag, object>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance(new string('a', 65), new Dictionary<DicomTag, object>()));
            Assert.Throws<ArgumentException>(() => new QueryInstance("AA#AA", new Dictionary<DicomTag, object>()));
            Assert.Throws<ArgumentNullException>(() => new QueryInstance(Guid.NewGuid().ToString(), null));

            Assert.Throws<ArgumentNullException>(() => QueryInstance.Create(null, null));
            Assert.Throws<ArgumentException>(() => QueryInstance.Create(new DicomDataset(), null));
        }

        [Fact]
        public void GivenDicomTagsToIndex_WhenCreatingQueryInstance_TagsAreExtractedAndIndexed()
        {
            var sequenceDataset = new DicomDataset()
            {
                { DicomTag.ReferringPhysicianName, "TestPhysician" },
            };

            var dicomDataset = new DicomDataset
            {
                { DicomTag.StudyInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.SOPInstanceUID, Guid.NewGuid().ToString() },
                { DicomTag.PatientName, Guid.NewGuid().ToString() },
                { DicomTag.StudyDate, DateTime.UtcNow },
            };
            dicomDataset.Add(sequenceDataset);

            var instance = QueryInstance.Create(
                dicomDataset, new[] { DicomTag.PatientName, DicomTag.StudyDate, DicomTag.ReferringPhysicianName, DicomTag.StudyTime });

            Assert.NotNull(instance);
            Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID), instance.SopInstanceUID);

            Assert.Equal(3, instance.IndexedAttributes.Count);
            Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.PatientName), instance.IndexedAttributes[DicomTag.PatientName]);
            Assert.Equal(dicomDataset.GetSingleValue<DateTime>(DicomTag.StudyDate), instance.IndexedAttributes[DicomTag.StudyDate]);
            Assert.Equal(dicomDataset.GetSingleValue<string>(DicomTag.ReferringPhysicianName), instance.IndexedAttributes[DicomTag.ReferringPhysicianName]);
        }
    }
}
