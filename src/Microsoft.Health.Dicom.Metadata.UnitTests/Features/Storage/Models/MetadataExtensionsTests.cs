// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Metadata.UnitTests.Features.Storage.Models
{
    public class MetadataExtensionsTests
    {
        private DicomMetadataConfiguration _dicomMetadataConfiguration = new DicomMetadataConfiguration();

        [Fact]
        public void GivenDicomStudyMetadata_WhenGettingOrRemovingInstancesWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            var studyMetadata = new DicomStudyMetadata(TestUidGenerator.Generate());
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.GetDicomInstances(null).ToList());
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.GetDicomInstances(studyMetadata, null));
            Assert.Throws<ArgumentException>(() => MetadataExtensions.GetDicomInstances(studyMetadata, string.Empty));
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.GetDicomInstances(null, TestUidGenerator.Generate()));
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.TryRemoveInstance(null, DicomInstance.Create(CreateDicomDataset())));
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.TryRemoveInstance(studyMetadata, null));

            var differentStudyInstanceUID = new DicomInstance(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate());
            Assert.Throws<ArgumentException>(() => MetadataExtensions.TryRemoveInstance(studyMetadata, differentStudyInstanceUID));
        }

        [Fact]
        public void GivenDicomStudyMetadata_WhenAddingInstanceWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            var studyMetadata = new DicomStudyMetadata(TestUidGenerator.Generate());
            Assert.Throws<ArgumentNullException>(() => MetadataExtensions.AddDicomInstance(null, new DicomDataset(), Array.Empty<DicomAttributeId>()));
            Assert.Throws<ArgumentNullException>(() => studyMetadata.AddDicomInstance(null, Array.Empty<DicomAttributeId>()));
            Assert.Throws<ArgumentNullException>(() => studyMetadata.AddDicomInstance(new DicomDataset(), null));
            Assert.Throws<ArgumentException>(() => studyMetadata.AddDicomInstance(CreateDicomDataset(), _dicomMetadataConfiguration.StudySeriesMetadataAttributes));
        }

        [Fact]
        public void GivenDicomStudyMetadata_WhenAddingDicomInstance_CorrectInstancesRetrieved()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            var studyMetadata = new DicomStudyMetadata(studyInstanceUID);

            DicomDataset dataset1 = CreateDicomDataset(studyInstanceUID);
            var instance1 = DicomInstance.Create(dataset1);
            studyMetadata.AddDicomInstance(dataset1, _dicomMetadataConfiguration.StudySeriesMetadataAttributes);

            DicomDataset dataset2 = CreateDicomDataset(studyInstanceUID, TestUidGenerator.Generate());
            var instance2 = DicomInstance.Create(dataset2);
            DicomDataset dataset3 = CreateDicomDataset(studyInstanceUID, instance2.SeriesInstanceUID);
            var instance3 = DicomInstance.Create(dataset3);
            studyMetadata.AddDicomInstance(dataset2, _dicomMetadataConfiguration.StudySeriesMetadataAttributes);
            studyMetadata.AddDicomInstance(dataset3, _dicomMetadataConfiguration.StudySeriesMetadataAttributes);

            DicomDataset dataset4 = CreateDicomDataset(studyInstanceUID);
            var instance4 = DicomInstance.Create(dataset4);
            studyMetadata.AddDicomInstance(dataset4, _dicomMetadataConfiguration.StudySeriesMetadataAttributes);

            Assert.Equal(3, studyMetadata.SeriesMetadata.Count);
            IEnumerable<DicomInstance> instancesInStudy = studyMetadata.GetDicomInstances();
            Assert.Equal(4, instancesInStudy.Count());
            Assert.Contains(instance1, instancesInStudy);
            Assert.Contains(instance2, instancesInStudy);
            Assert.Contains(instance3, instancesInStudy);
            Assert.Contains(instance4, instancesInStudy);

            IEnumerable<DicomInstance> instancesInSeries1 = studyMetadata.GetDicomInstances(instance1.SeriesInstanceUID);
            Assert.Single(instancesInSeries1);
            Assert.Equal(instance1, instancesInSeries1.First());

            IEnumerable<DicomInstance> instancesInSeries2 = studyMetadata.GetDicomInstances(instance2.SeriesInstanceUID);
            Assert.Equal(2, instancesInSeries2.Count());
            Assert.Contains(instance2, instancesInSeries2);
            Assert.Contains(instance3, instancesInSeries2);

            IEnumerable<DicomInstance> instancesInSeries3 = studyMetadata.GetDicomInstances(instance4.SeriesInstanceUID);
            Assert.Single(instancesInSeries3);
            Assert.Equal(instance4, instancesInSeries3.First());
        }

        [Fact]
        public void GivenDicomStudyMetadata_WhenRemovingDicomInstance_CorrectInstancesRetrieved()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            var studyMetadata = new DicomStudyMetadata(studyInstanceUID);
            DicomAttributeId[] indexableAttributes = new[] { new DicomAttributeId(DicomTag.PatientName) };

            IList<DicomInstance> instances = Enumerable.Range(0, 4).Select(x =>
            {
                DicomDataset dataset = CreateDicomDataset(studyInstanceUID);
                studyMetadata.AddDicomInstance(dataset, indexableAttributes);
                return DicomInstance.Create(dataset);
            }).ToList();

            Assert.False(studyMetadata.TryRemoveInstance(new DicomInstance(studyInstanceUID, TestUidGenerator.Generate(), TestUidGenerator.Generate())));
            Assert.False(studyMetadata.TryRemoveInstance(new DicomInstance(studyInstanceUID, instances[0].SeriesInstanceUID, TestUidGenerator.Generate())));
            Assert.False(studyMetadata.TryRemoveInstance(new DicomInstance(studyInstanceUID, TestUidGenerator.Generate(), instances[0].SopInstanceUID)));
            Assert.True(studyMetadata.TryRemoveInstance(instances[0]));
            Assert.False(studyMetadata.TryRemoveInstance(instances[0]));
            Assert.Equal(3, studyMetadata.GetDicomInstances().Count());

            Assert.True(studyMetadata.TryRemoveInstance(instances[1]));
            Assert.Equal(2, studyMetadata.GetDicomInstances().Count());

            Assert.True(studyMetadata.TryRemoveInstance(instances[2]));
            Assert.False(studyMetadata.TryRemoveInstance(instances[2]));
            IEnumerable<DicomInstance> instancesInStudy = studyMetadata.GetDicomInstances();
            Assert.Single(instancesInStudy);
            Assert.Equal(instances[3], instancesInStudy.First());

            Assert.True(studyMetadata.TryRemoveInstance(instances[3]));
            Assert.False(studyMetadata.TryRemoveInstance(instances[3]));

            Assert.Empty(studyMetadata.GetDicomInstances());
            Assert.Throws<ArgumentException>(() => studyMetadata.GetDicomInstances(instances[3].SeriesInstanceUID));
        }

        [Fact]
        public void GivenDicomStudyMetadata_WhenAddingDicomInstances_CorrectDicomItemsAreStored()
        {
            var testPatientName = "Mr^Test^User";
            var personIdentificationCodeSequence = new DicomSequence(
                DicomTag.PersonIdentificationCodeSequence,
                new DicomDataset()
                {
                    { DicomTag.InstitutionName, "TestInstitution" },
                    new DicomSequence(DicomTag.InstitutionCodeSequence, new DicomDataset() { { DicomTag.CodeMeaning, "TestMeaning" } }),
                });
            var studyInstanceUID = TestUidGenerator.Generate();
            var seriesInstanceUID = TestUidGenerator.Generate();
            DicomAttributeId[] indexableAttributes = new[]
            {
                new DicomAttributeId(DicomTag.PatientName),
                new DicomAttributeId(DicomTag.StudyDescription),
                new DicomAttributeId(DicomTag.PersonIdentificationCodeSequence),
                new DicomAttributeId(DicomTag.ReferringPhysicianIdentificationSequence),
                new DicomAttributeId(DicomTag.StudyDate),
                new DicomAttributeId(DicomTag.StudyTime),
            };

            DicomDataset dicomDataset1 = CreateDicomDataset(studyInstanceUID, seriesInstanceUID);
            dicomDataset1.AddOrUpdate(DicomTag.PatientName, testPatientName);
            dicomDataset1.AddOrUpdate(DicomTag.StudyDescription, "Test Description 1");
            dicomDataset1.AddOrUpdate(DicomTag.StudyDate, new DateTime(2019, 7, 15));
            dicomDataset1.AddOrUpdate(DicomTag.StudyID, "NOTINDEXED");
            dicomDataset1.AddOrUpdate(personIdentificationCodeSequence);
            dicomDataset1.AddOrUpdate(new DicomSequence(
                DicomTag.ReferringPhysicianIdentificationSequence,
                new DicomDataset()
                {
                    { DicomTag.PersonAddress, "Test Address" },
                },
                new DicomDataset()
                {
                    { DicomTag.PersonAddress, "Test Address" },
                    { DicomTag.PersonName, "Test^Name" },
                }));

            DicomDataset dicomDataset2 = CreateDicomDataset(studyInstanceUID, seriesInstanceUID);
            dicomDataset2.AddOrUpdate(DicomTag.PatientName, testPatientName);
            dicomDataset2.AddOrUpdate(DicomTag.StudyDescription, "Test Description 2");
            dicomDataset2.AddOrUpdate(DicomTag.StudyDate, new DateTime(2019, 7, 14));
            dicomDataset2.AddOrUpdate(DicomTag.PatientSex, "M");
            dicomDataset2.AddOrUpdate(personIdentificationCodeSequence);
            dicomDataset2.AddOrUpdate(new DicomSequence(
                DicomTag.ReferringPhysicianIdentificationSequence,
                new DicomDataset()
                {
                    { DicomTag.PersonAddress, "Test Address" },
                }));

            var studyMetadata = new DicomStudyMetadata(studyInstanceUID);
            studyMetadata.AddDicomInstance(dicomDataset1, indexableAttributes);
            studyMetadata.AddDicomInstance(dicomDataset2, indexableAttributes);

            Assert.Equal(1, studyMetadata.SeriesMetadata.Count);
            DicomSeriesMetadata seriesMetadata = studyMetadata.SeriesMetadata[seriesInstanceUID];
            Assert.Null(seriesMetadata.DicomItems.FirstOrDefault(x => x.DicomItem.Tag == DicomTag.StudyTime));
            Assert.Null(seriesMetadata.DicomItems.FirstOrDefault(x => x.DicomItem.Tag == DicomTag.PatientSex));

            var patientNames = seriesMetadata.DicomItems.Where(x => x.DicomItem.Tag == DicomTag.PatientName).ToList();
            Assert.Single(patientNames);
            Assert.Equal(2, patientNames[0].Instances.Count);
            Assert.Contains(1, patientNames[0].Instances);
            Assert.Contains(2, patientNames[0].Instances);

            var personIdentificationCodeSequences = seriesMetadata.DicomItems.Where(x => x.DicomItem.Tag == DicomTag.PersonIdentificationCodeSequence).ToList();
            Assert.Single(personIdentificationCodeSequences);
            Assert.Equal(2, personIdentificationCodeSequences[0].Instances.Count);
            Assert.Contains(1, personIdentificationCodeSequences[0].Instances);
            Assert.Contains(2, personIdentificationCodeSequences[0].Instances);

            var studyDescriptions = seriesMetadata.DicomItems.Where(x => x.DicomItem.Tag == DicomTag.StudyDescription).ToList();
            Assert.Equal(2, studyDescriptions.Count);
            Assert.Contains(1, studyDescriptions[0].Instances);
            Assert.DoesNotContain(2, studyDescriptions[0].Instances);
            Assert.Contains(2, studyDescriptions[1].Instances);
            Assert.DoesNotContain(1, studyDescriptions[1].Instances);

            Assert.Equal(2, seriesMetadata.DicomItems.Count(x => x.DicomItem.Tag == DicomTag.StudyDate));
            Assert.Equal(2, seriesMetadata.DicomItems.Count(x => x.DicomItem.Tag == DicomTag.ReferringPhysicianIdentificationSequence));
        }

        private DicomDataset CreateDicomDataset(string studyInstanceUID = null, string seriesInstanceUID = null)
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, studyInstanceUID ?? TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, seriesInstanceUID ?? TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
            };
        }
    }
}
