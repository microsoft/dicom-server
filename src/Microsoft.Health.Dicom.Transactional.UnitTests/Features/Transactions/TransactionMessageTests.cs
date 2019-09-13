// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Transactional.Features.Transactions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Dicom.Transactional.UnitTests.Features.Transactions
{
    public class TransactionMessageTests
    {
        [Fact]
        public void GivenTransactionMessage_WhenConstructingWithInvalidArguments_ArgumentExceptionIsThrown()
        {
            DicomSeries dicomSeries = CreateDicomSeries();
            Assert.Throws<ArgumentNullException>(() => new TransactionMessage(null, new HashSet<DicomInstance>()));
            Assert.Throws<ArgumentNullException>(() => new TransactionMessage(dicomSeries, null));
        }

        [Fact]
        public void GivenTransactionMessage_WhenAddingInstanceWithInvalidArguments_ArgumentExceptionIsThrown()
        {
            DicomSeries dicomSeries = CreateDicomSeries();
            var transactionMessage = new TransactionMessage(dicomSeries, new HashSet<DicomInstance>());

            Assert.Throws<ArgumentNullException>(() => transactionMessage.AddInstance(null));
            Assert.Throws<ArgumentException>(() => transactionMessage.AddInstance(new DicomInstance(dicomSeries.StudyInstanceUID, DicomUID.Generate().UID, DicomUID.Generate().UID)));
            Assert.Throws<ArgumentException>(() => transactionMessage.AddInstance(new DicomInstance(DicomUID.Generate().UID, dicomSeries.SeriesInstanceUID, DicomUID.Generate().UID)));
        }

        [Fact]
        public void GivenTransactionMessage_WhenConstructingWithMultipleInstancesForDifferenceSeries_ArgumentExceptionIsThrown()
        {
            DicomSeries dicomSeries1 = CreateDicomSeries();
            DicomSeries dicomSeries2 = CreateDicomSeries();
            DicomInstance instance1 = CreateDicomInstance(dicomSeries1);
            DicomInstance instance2 = CreateDicomInstance(dicomSeries1);
            DicomInstance instance3 = CreateDicomInstance(dicomSeries2);

            Assert.Throws<ArgumentException>(() => new TransactionMessage(dicomSeries1, new HashSet<DicomInstance>(new[] { instance1, instance2, instance3 })));
        }

        [Fact]
        public void GivenValidTransactionMessage_WhenSerialized_IsDeserializedCorrectly()
        {
            DicomSeries dicomSeries1 = CreateDicomSeries();
            DicomInstance instance1 = CreateDicomInstance(dicomSeries1);
            DicomInstance instance2 = CreateDicomInstance(dicomSeries1);
            DicomInstance instance3 = CreateDicomInstance(dicomSeries1);

            var expected = new TransactionMessage(dicomSeries1, new HashSet<DicomInstance>(new[] { instance1, instance2, instance3 }));
            var serialized = JsonConvert.SerializeObject(expected);
            TransactionMessage actual = JsonConvert.DeserializeObject<TransactionMessage>(serialized);

            Assert.Equal(expected.DicomSeries, actual.DicomSeries);

            DicomInstance[] expectedInstances = expected.Instances.ToArray();
            DicomInstance[] actualInstances = actual.Instances.ToArray();

            Assert.Equal(expectedInstances.Length, actualInstances.Length);

            for (var i = 0; i < expectedInstances.Length;  i++)
            {
                Assert.Equal(expectedInstances[i], actualInstances[i]);
            }
        }

        private DicomSeries CreateDicomSeries()
            => new DicomSeries(DicomUID.Generate().UID, DicomUID.Generate().UID);

        private DicomInstance CreateDicomInstance(DicomSeries dicomSeries)
            => new DicomInstance(dicomSeries.StudyInstanceUID, dicomSeries.SeriesInstanceUID, DicomUID.Generate().UID);
    }
}
