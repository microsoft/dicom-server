// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents;
using Microsoft.Health.Dicom.Tests.Common;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.CosmosDb.UnitTests.Features.Storage.Documents
{
    public class QuerySeriesDocumentTests
    {
        [Fact]
        public void GivenInvalidInstanceIdentifers_WhenCreatingQuerySeriesDocument_ExceptionsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new QuerySeriesDocument(null, TestUidGenerator.Generate()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(string.Empty, TestUidGenerator.Generate()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(new string('a', 65), TestUidGenerator.Generate()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument("?...", TestUidGenerator.Generate()));
            Assert.Throws<ArgumentNullException>(() => new QuerySeriesDocument(TestUidGenerator.Generate(), null));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(TestUidGenerator.Generate(), string.Empty));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument("sameid", "sameid"));

            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetDocumentId(null, TestUidGenerator.Generate()));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId(string.Empty, TestUidGenerator.Generate()));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(TestUidGenerator.Generate(), new string('a', 65)));
            Assert.Throws<ArgumentException>(() => new QuerySeriesDocument(TestUidGenerator.Generate(), "?..."));
            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetDocumentId(TestUidGenerator.Generate(), null));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId(TestUidGenerator.Generate(), string.Empty));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetDocumentId("sameid", "sameid"));

            Assert.Throws<ArgumentNullException>(() => QuerySeriesDocument.GetPartitionKey(null));
            Assert.Throws<ArgumentException>(() => QuerySeriesDocument.GetPartitionKey(string.Empty));
        }

        [Fact]
        public void GivenExistingInstance_WhenAddingToInstancesHashSet_IsNotAdded()
        {
            var document = new QuerySeriesDocument(TestUidGenerator.Generate(), TestUidGenerator.Generate());

            var dataset = new DicomDataset();
            var sopInstanceUID = TestUidGenerator.Generate();
            var testPatientName = Guid.NewGuid().ToString();
            dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUID);
            dataset.Add(DicomTag.PatientName, testPatientName);

            var patientNameAttributeId = new DicomAttributeId(DicomTag.PatientName);
            var instanceDocument1 = QueryInstance.Create(dataset, new[] { patientNameAttributeId });
            var instanceDocument2 = QueryInstance.Create(dataset, new[] { patientNameAttributeId });

            Assert.Throws<ArgumentNullException>(() => document.AddInstance(null));
            Assert.True(document.AddInstance(instanceDocument1));
            Assert.False(document.AddInstance(instanceDocument2));

            Assert.Equal(testPatientName, document.DistinctAttributes[patientNameAttributeId.AttributeId].Values.First());

            Assert.Throws<ArgumentNullException>(() => document.RemoveInstance(null));
            Assert.Throws<ArgumentException>(() => document.RemoveInstance(string.Empty));
            Assert.True(document.RemoveInstance(sopInstanceUID));
            Assert.False(document.RemoveInstance(sopInstanceUID));
        }

        [Fact]
        public void GivenSeriesDocument_WhenSerialized_IsDeserializedCorrectly()
        {
            var document = new QuerySeriesDocument(TestUidGenerator.Generate(), TestUidGenerator.Generate())
            {
                ETag = Guid.NewGuid().ToString(),
            };

            var dataset = new DicomDataset();
            var sopInstanceUID = TestUidGenerator.Generate();
            var testPatientName = Guid.NewGuid().ToString();
            dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUID);
            dataset.Add(DicomTag.PatientName, testPatientName);

            var patientNameAttributeId = new DicomAttributeId(DicomTag.PatientName);
            var instanceDocument = QueryInstance.Create(dataset, new[] { patientNameAttributeId });
            document.AddInstance(instanceDocument);

            var serialized = JsonConvert.SerializeObject(document);
            QuerySeriesDocument deserialized = JsonConvert.DeserializeObject<QuerySeriesDocument>(serialized);

            Assert.Equal(document.Id, deserialized.Id);
            Assert.Equal(document.PartitionKey, deserialized.PartitionKey);
            Assert.Equal(document.ETag, deserialized.ETag);
            Assert.Equal(document.StudyUID, deserialized.StudyUID);
            Assert.Equal(document.SeriesUID, deserialized.SeriesUID);
            Assert.Equal(document.Instances.Count, deserialized.Instances.Count);

            QueryInstance deserializedFirstInstance = deserialized.Instances.First();
            Assert.Equal(deserializedFirstInstance.InstanceUID, deserializedFirstInstance.InstanceUID);
            Assert.Equal(deserializedFirstInstance.Attributes.Count, deserializedFirstInstance.Attributes.Count);
            Assert.Equal(deserializedFirstInstance.Attributes[patientNameAttributeId.AttributeId], deserializedFirstInstance.Attributes[patientNameAttributeId.AttributeId]);
        }
    }
}
