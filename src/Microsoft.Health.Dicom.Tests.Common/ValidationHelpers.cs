// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class ValidationHelpers
    {
        public static void ValidateFailureSequence(DicomSequence dicomSequence, ushort expectedFailureCode, params DicomDataset[] expectedFailedDatasets)
        {
            Assert.Equal(DicomTag.FailedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedFailedDatasets.Length);
            var expectedSopClassUIDs = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPClassUID, string.Empty)));
            var expectedSopInstanceUIDs = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty)));

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                Assert.Equal(expectedFailureCode, dataset.GetSingleValue<ushort>(DicomTag.FailureReason));
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID), expectedSopClassUIDs);
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID), expectedSopInstanceUIDs);
            }
        }

        public static void ValidateSuccessSequence(DicomSequence dicomSequence, params DicomDataset[] expectedDatasets)
        {
            Assert.Equal(DicomTag.ReferencedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedDatasets.Length);
            var datasetsBySopInstanceUID = expectedDatasets.ToDictionary(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID), x => x);

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                var referencedSopInstanceUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.True(datasetsBySopInstanceUID.ContainsKey(referencedSopInstanceUID));
                DicomDataset referenceDataset = datasetsBySopInstanceUID[referencedSopInstanceUID];
                var dicomInstance = DicomInstance.Create(referenceDataset);
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.SOPClassUID), dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID));
                Assert.EndsWith(
                    $"studies/{dicomInstance.StudyInstanceUID}/series/{dicomInstance.SeriesInstanceUID}/instances/{dicomInstance.SopInstanceUID}",
                    dataset.GetSingleValue<string>(DicomTag.RetrieveURL));
            }
        }
    }
}
